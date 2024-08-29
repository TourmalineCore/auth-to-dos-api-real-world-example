using System.Security.Claims;
using DataAccess.Queries;
using TourmalineCore.AspNetCore.JwtAuthentication.Core.Contract;

namespace Api.Services.Users
{
    public class UserClaimsProvider : IUserClaimsProvider
    {
        private readonly IFindUserQuery _userQuery;
        private readonly ILogger<UserClaimsProvider> _logger;
        private readonly IExternalServicesHttpClient _externalServicesHttpClient;

        public const string PermissionsClaimType = "permissions";

        public const string TenantIdClaimType = "tenantId";

        private const string NameIdentifierClaimType = "nameIdentifier";

        private const string LoginClaimType = "login";

        public UserClaimsProvider(
            IFindUserQuery userQuery,
            ILogger<UserClaimsProvider> logger,
            IExternalServicesHttpClient externalServicesHttpClient)
        {
            _userQuery = userQuery;
            _logger = logger;
            _externalServicesHttpClient = externalServicesHttpClient;
        }

        public async Task<List<Claim>> GetUserClaimsAsync(string login)
        {
            var user = await _userQuery.FindUserByLoginAsync(login);
            var privileges = await _externalServicesHttpClient.GetPermissions(user.AccountId);
            var tenantId = await _externalServicesHttpClient.GetTenantId(user.AccountId);

            var claims = new List<Claim>
            {
                new (NameIdentifierClaimType, login),
                new (LoginClaimType, user.UserName),
                new (TenantIdClaimType, tenantId.ToString())

            };
            privileges.ForEach(x => claims.Add(new Claim(PermissionsClaimType, x.ToString())));

            return claims;
        }
    }
}