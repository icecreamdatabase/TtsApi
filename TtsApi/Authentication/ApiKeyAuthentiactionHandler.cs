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
using TtsApi.Model;

// this disables the warning about not using async.
// I'm overriding, so therefore can't change the return type to none Task<T>
#pragma warning disable 1998

namespace TtsApi.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private const string ApiKeyHeaderName = "Authorization";
        private readonly TtsDbContext _ttsDbContext;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            TtsDbContext ttsDbContext
        ) : base(options, logger, encoder, clock)
        {
            _ttsDbContext = ttsDbContext;
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

            SetRoles(validate, claims);

            ClaimsIdentity identity = new(claims, ApiKeyAuthenticationOptions.AuthenticationType);
            List<ClaimsIdentity> identities = new() {identity};
            ClaimsPrincipal principal = new(identities);
            AuthenticationTicket ticket = new(principal, ApiKeyAuthenticationOptions.Scheme);

            return AuthenticateResult.Success(ticket);
        }

        private void SetRoles(TwitchValidateResult validate, ICollection<Claim> claims)
        {
            if (!int.TryParse(validate.UserId, out int userId))
                return;

            // Get channelId from Route and userId from validate
            if (
                !Request.RouteValues.TryGetValue("channelId", out object channelIdStr) ||
                channelIdStr == null ||
                !int.TryParse(channelIdStr.ToString(), out int channelId)
            )
                return;

            // Bot check
            if (new[] {1234}.Contains(userId))
                claims.Add(new Claim(ClaimTypes.Role, Roles.Roles.ChatBot));

            // Admin check
            if (new[] {1234}.Contains(userId))
                claims.Add(new Claim(ClaimTypes.Role, Roles.Roles.Admin));

            // Broadcaster check
            if (channelId == userId)
                claims.Add(new Claim(ClaimTypes.Role, Roles.Roles.ChannelBroadcaster));

            // Mod check
            // TODO: Mod check
            if (channelId == 1234)
                claims.Add(new Claim(ClaimTypes.Role, Roles.Roles.ChannelMod));
        }
    }
}
