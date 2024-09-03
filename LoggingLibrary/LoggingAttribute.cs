using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Filters;

using Serilog.Context;

namespace LoggingLibrary;

public class LoggingAttribute : ActionFilterAttribute
{
    private readonly string _projectName;

    public LoggingAttribute(string projectName)
    {
        _projectName = projectName;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        var noLogging = context
        .ActionDescriptor
        .EndpointMetadata
        .OfType<NoLoggingAttribute>()
        .Any();

        if (noLogging)
        {
            await next();
            return;
        }

        var headers = new AttributeContext(Activity.Current, context.HttpContext);
        using (LogContext.PushProperty("SentryTrace", headers.SentryTraceHeader))

        {
            await next();
        }
    }
}