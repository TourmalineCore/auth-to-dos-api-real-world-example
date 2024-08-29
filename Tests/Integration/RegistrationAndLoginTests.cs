using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Integration;

public class RegistrationAndLoginTests : TestBase
{
    private const string Login = "admin";
    private const string Password = "admin";

    public RegistrationAndLoginTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithInvalidCreds()
    {
        var registrationResponse = await LoginAsync(Login, "1");

        Assert.NotEqual(HttpStatusCode.OK, registrationResponse.response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCreds()
    {
        var registrationResponse = await LoginAsync("invalid", "invalid");

        Assert.Equal(HttpStatusCode.Unauthorized, registrationResponse.response.StatusCode);
    }
}