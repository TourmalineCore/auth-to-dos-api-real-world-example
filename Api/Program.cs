using System.Reflection;
using System.Runtime.InteropServices;
using Api.Options;
using Api.Services;
using Api.Services.Callbacks;
using Api.Services.Options;
using Api.Services.Users;
using DataAccess;
using DataAccess.Commands;
using DataAccess.Models;
using DataAccess.Queries;
using LoggingLibrary;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.EventLog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TourmalineCore.AspNetCore.JwtAuthentication.Core;
using TourmalineCore.AspNetCore.JwtAuthentication.Core.Options;
using TourmalineCore.AspNetCore.JwtAuthentication.Identity;
using TourmalineCore.AspNetCore.JwtAuthentication.Identity.Options;
using AuthenticationOptions = TourmalineCore.AspNetCore.JwtAuthentication.Core.Options.AuthenticationOptions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
const string defaultConnection = "DefaultConnection";
// configure ElasticSearch credentials
builder.Services.Configure<ElasticSearchOptions>(configuration.GetSection("ElasticSearchOptions"));
var elasticSearchOptions = configuration
    .GetSection(nameof(ElasticSearchOptions))
    .Get<ElasticSearchOptions>();

// add logging
builder.Services.AddScoped(_ => new LoggingAttribute("Api"));
ElkLogger.SetupLogger(
    elasticSearchOptions.ElasticSearchUrl,
    elasticSearchOptions.ElasticSearchLogin,
    elasticSearchOptions.ElasticSearchPassword);

builder.Services.AddControllers();
// add tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .AddSource("Logs.Startup")
            .SetSampler(new AlwaysOnSampler())
            .SetResourceBuilder(
                ResourceBuilder
                    .CreateDefault()
                    .AddService("OpenTelemetry.RampUp.Auth.*", serviceVersion: "0.0.1"))
            .AddAspNetCoreInstrumentation()
            .AddJaegerExporter(o =>
            {
                o.AgentHost = Environment.GetEnvironmentVariable("JAEGER_HOST") ?? "localhost";
                o.AgentPort = int.TryParse(Environment.GetEnvironmentVariable("JAEGER_PORT"), out var port) ? port : 6831;
            })

            .AddConsoleExporter();
    });
builder.Services.AddCors();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    var env = hostingContext.HostingEnvironment;

    var reloadOnChange = hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", true);

    config.AddJsonFile("appsettings.json", true, reloadOnChange)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, reloadOnChange)
        .AddJsonFile("appsettings.Active.json", true, reloadOnChange);

    if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
    {
        var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));

        config.AddUserSecrets(appAssembly, true);
    }

    config.AddEnvironmentVariables();

    if (args != null) config.AddCommandLine(args);
});

builder.Host.ConfigureLogging((hostingContext, logging) =>
{
    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // IMPORTANT: This needs to be added *before* configuration is loaded, this lets
    // the defaults be overridden by the configuration.
    if (isWindows)
        // Default the EventLogLoggerProvider to warning or above
        logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);

    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
    logging.AddConsole();
    logging.AddDebug();
    logging.AddEventSourceLogger();

    if (isWindows)
        // Add the EventLogLoggerProvider on windows machines
        logging.AddEventLog();

    logging.Configure(options =>
        {
            options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                              | ActivityTrackingOptions.TraceId
                                              | ActivityTrackingOptions.ParentId;
        }
    );
});

builder
    .Services.AddDbContext<AppDbContext>(options =>
        {
            AppDbContext.ConfigureContextOptions(options, configuration.GetConnectionString(defaultConnection));
        }
    );

var authenticationOptions = configuration.GetSection(nameof(AuthenticationOptions)).Get<RefreshAuthenticationOptions>();

builder.Services
    .AddJwtAuthenticationWithIdentity<AppDbContext, User, long>()
    .AddLoginWithRefresh(authenticationOptions)
    .AddRefreshConfidenceInterval()
    .AddLogout()
    .AddUserCredentialsValidator<UserCredentialsValidator>()
    .WithUserClaimsProvider<UserClaimsProvider>(UserClaimsProvider.PermissionsClaimType);

builder.Services.AddIdentityCore<User>().AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
});

var innerCircleServiceUrl = configuration.GetSection("ExternalServicesUrls");
builder.Services.Configure<ExternalServicesUrls>(u => innerCircleServiceUrl.Bind(u));

builder.Services.AddSingleton<AuthCallbacks>();
builder.Services.AddTransient<IFindUserQuery, FindUserQuery>();
builder.Services.AddTransient<GetUserQuery>();
builder.Services.AddTransient<UsersService>();
builder.Services.AddTransient<IPasswordValidator<User>, PasswordValidator<User>>();
builder.Services.AddTransient<UserBlockCommand>();
builder.Services.AddTransient<UserUnblockCommand>();

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Debug")
    builder.Services.AddTransient<IExternalServicesHttpClient, ExternalServicesHttpClient>();
else
    // toDo: change FakeExternalServicesHttpClient to ExternalServicesHttpClient
    builder.Services.AddTransient<IExternalServicesHttpClient, ExternalServicesHttpClient>();


var app = builder.Build();

app.UseCors(
    corsPolicyBuilder => corsPolicyBuilder
        .AllowAnyHeader()
        .SetIsOriginAllowed(host => true)
        .AllowAnyMethod()
        .AllowAnyOrigin()
);

app.UseSwagger();
app.UseSwaggerUI();

using var serviceScope = app.Services.CreateScope();

app
    .OnLoginExecuting(serviceScope.ServiceProvider.GetRequiredService<AuthCallbacks>().OnLoginExecuting)
    .OnLoginExecuted(serviceScope.ServiceProvider.GetRequiredService<AuthCallbacks>().OnLoginExecuted)
    .UseDefaultLoginMiddleware(new LoginEndpointOptions
    {
        LoginEndpointRoute = "/api/auth/login"
    }
    )
    .UseRefreshTokenMiddleware(new RefreshEndpointOptions
    {
        RefreshEndpointRoute = "/api/auth/refresh"
    }
    )
    .UseRefreshTokenLogoutMiddleware(new LogoutEndpointOptions
    {
        LogoutEndpointRoute = "/api/auth/logout"
    }
    );

var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
context.Database.Migrate();

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.Run();

public partial class Program
{
}