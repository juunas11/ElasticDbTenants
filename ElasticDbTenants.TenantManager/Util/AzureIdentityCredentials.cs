using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Rest;

namespace ElasticDbTenants.TenantManager.Util
{
    /// <summary>
    /// Authenticates requests to Azure
    /// management APIs with the given
    /// Azure AD credential.
    /// </summary>
    public class AzureIdentityCredentials : ServiceClientCredentials
    {
        private static readonly string[] Scopes = new[] { "https://management.core.windows.net/.default" };
        private readonly TokenCredential _tokenCredential;
        private AccessToken _cachedToken;

        public AzureIdentityCredentials(TokenCredential tokenCredential)
        {
            _tokenCredential = tokenCredential;
        }

        public override async Task ProcessHttpRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (_cachedToken.ExpiresOn > DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10))
            {
                return _cachedToken.Token;
            }

            var token = await _tokenCredential.GetTokenAsync(
                new TokenRequestContext(Scopes),
                cancellationToken);

            _cachedToken = token;

            return token.Token;
        }
    }
}