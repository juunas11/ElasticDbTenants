using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticDbTenants.Db.Common
{
    /// <summary>
    /// Authenticates connections to SQL
    /// database with Azure AD access tokens.
    /// </summary>
    public class EfAzureAdInterceptor : DbConnectionInterceptor
    {
        private readonly TokenCredential _tokenCredential;
        private AccessToken _cachedToken;

        public EfAzureAdInterceptor(TokenCredential tokenCredential)
        {
            _tokenCredential = tokenCredential;
        }

        public override async Task<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            var accessToken = await AcquireAccessTokenAsync(cancellationToken);
            ((SqlConnection)connection).AccessToken = accessToken;
            return result;
        }

        public override InterceptionResult ConnectionOpening(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result)
        {
            var accessToken = AcquireAccessToken();
            ((SqlConnection)connection).AccessToken = accessToken;
            return result;
        }

        private async ValueTask<string> AcquireAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (_cachedToken.ExpiresOn > DateTime.UtcNow.AddMinutes(5))
            {
                return _cachedToken.Token;
            }

            _cachedToken = await _tokenCredential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://database.windows.net/.default" }),
                cancellationToken);
            return _cachedToken.Token;
        }

        private string AcquireAccessToken()
        {
            if (_cachedToken.ExpiresOn > DateTime.UtcNow.AddMinutes(5))
            {
                return _cachedToken.Token;
            }

            _cachedToken = _tokenCredential.GetToken(
                new TokenRequestContext(new[] { "https://database.windows.net/.default" }),
                default);
            return _cachedToken.Token;
        }
    }
}
