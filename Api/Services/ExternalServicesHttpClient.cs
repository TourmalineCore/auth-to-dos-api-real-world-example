using System.Text.Json;
using System.Web;
using Api.Services.Options;
using Microsoft.Extensions.Options;

namespace Api.Services
{
    public class ExternalServicesHttpClient : IExternalServicesHttpClient
    {
        private readonly HttpClient _client;
        private readonly ExternalServicesUrls _urls;

        public ExternalServicesHttpClient(IOptions<ExternalServicesUrls> urls)
        {
            _client = new HttpClient();
            _urls = urls.Value;
        }

        public async Task SendPasswordCreationLink(string login, string passwordResetToken)
        {
            var mailSenderLink = $"{_urls.MailServiceUrl}/mail/send-welcome-link";
            await _client.PostAsJsonAsync(mailSenderLink,
                new
                {
                    To = login,
                    Subject = "Change your password to ToDos",
                    Body =
                        $"Go to this link to set a password for your account: {_urls.AuthUIServiceUrl}/change-password?passwordResetToken={HttpUtility.UrlEncode(passwordResetToken)}&login={login}"
                });
        }

        public async Task SendPasswordResetLink(string login, string passwordResetToken)
        {
            var mailSenderLink = $"{_urls.MailServiceUrl}/mail/send-reset-link";
            await _client.PostAsJsonAsync(mailSenderLink,
                new
                {
                    To = login,
                    Subject = "Reset your password to Tourmaline Core",
                    Body =
                        $"Go to this link to reset a password for your account: {_urls.AuthUIServiceUrl}/change-password?passwordResetToken={HttpUtility.UrlEncode(passwordResetToken)}&login={login}"
                });
        }

        public async Task<List<string>> GetPermissions(long accountId)
        {
            var accountPermissionsLink = $"{_urls.AccountsServiceUrl}/internal/account-permissions/{accountId}";
            var response = await _client.GetStringAsync(accountPermissionsLink);
            return JsonSerializer.Deserialize<List<string>>(response);
        }

        public async Task<long> GetTenantId(long accountId)
        {
            var link = $"{_urls.AccountsServiceUrl}/internal/get-tenantId-by-accountId/{accountId}";
            var response = await _client.GetStringAsync(link);
            return JsonSerializer.Deserialize<long>(response);
        }
    }
}