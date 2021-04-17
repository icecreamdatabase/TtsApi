using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TtsApi.Authentication.Twitch;

// this disables the warning about not using async.
// I'm overriding, so therefore can't change the return type to none Task<T>
#pragma warning disable 1998

namespace TtsApi.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private const string ApiKeyHeaderName = "Authorization";

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
        ) : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out StringValues apiKeyHeaderValues))
            {
                return AuthenticateResult.NoResult();
            }
            string providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
            {
                return AuthenticateResult.NoResult();
            }

            TwitchValidateResult validate = TwitchOAuthHandler.Validate(providedApiKey);

            if (string.IsNullOrEmpty(validate.UserId))
            {
                //Response.StatusCode = validate.Status;
                //await Response.WriteAsync(validate.Message);
                return AuthenticateResult.NoResult();
            }

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, validate.Login),

                new Claim(AuthClaims.ClientId, validate.ClientId),
                new Claim(AuthClaims.Login, validate.Login),
                new Claim(AuthClaims.Scopes, string.Join(',', validate.Scopes)),
                new Claim(AuthClaims.UserId, validate.UserId),
                new Claim(AuthClaims.ExpiresIn, validate.ExpiresIn.ToString()),
            };

            ClaimsIdentity identity = new(claims, ApiKeyAuthenticationOptions.AuthenticationType);
            List<ClaimsIdentity> identities = new() {identity};
            ClaimsPrincipal principal = new(identities);
            AuthenticationTicket ticket = new(principal, ApiKeyAuthenticationOptions.Scheme);

            return AuthenticateResult.Success(ticket);
        }
    }
}
