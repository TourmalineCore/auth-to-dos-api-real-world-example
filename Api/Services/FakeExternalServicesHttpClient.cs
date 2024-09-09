namespace Api.Services;

public class FakeExternalServicesHttpClient
{
    private readonly ILogger<FakeExternalServicesHttpClient> _logger;

    public FakeExternalServicesHttpClient(ILogger<FakeExternalServicesHttpClient> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordCreationLink(string corporateEmail, string passwordResetToken)
    {
        _logger.LogInformation($"Corporate email: {corporateEmail}, password reset token: {passwordResetToken}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLink(string email, string token)
    {
        return Task.CompletedTask;
    }

    public async Task<List<string>> GetPermissions(long accountId)
    {
        return new List<string>
        {
            "CanViewFinanceForPayroll",
            "CanViewAnalytic",
            "CanManageEmployees"
        };
    }
}