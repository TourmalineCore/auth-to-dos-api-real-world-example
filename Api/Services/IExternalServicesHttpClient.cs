using Newtonsoft.Json.Linq;

namespace Api.Services
{
    public interface IExternalServicesHttpClient
    {
        Task SendPasswordCreationLink(string login, string passwordResetToken);
        Task SendPasswordResetLink(string login, string token);
        Task<List<string>> GetPermissions(long accountId);
        Task<long> GetTenantId(long accountId);
    }
}
