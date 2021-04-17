using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TtsApi.Authentication.Twitch;

namespace TtsApi.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private const string ProblemDetailsContentType = "application/problem+json";

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

            string? providedApiKey = apiKeyHeaderValues.FirstOrDefault();

            if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
            {
                return AuthenticateResult.NoResult();
            }

            //providedApiKey  
            TwitchValidateResult validate = TwitchOAuthHandler.Validate(providedApiKey);

            if (validate.UserId == null)
                return AuthenticateResult.NoResult();

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

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.ContentType = ProblemDetailsContentType;

            await Response.WriteAsync("401");
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            Response.ContentType = ProblemDetailsContentType;

            await Response.WriteAsync("403");
        }
    }
}
