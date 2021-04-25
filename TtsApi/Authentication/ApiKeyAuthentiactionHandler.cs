using System;
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

namespace TtsApi.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private const string AccessTokenQueryStringName = "access_token";
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
            StringValues oAuthHeader = "";
            if (
                !Context.Request.Query.TryGetValue(AccessTokenQueryStringName, out StringValues accessToken) &&
                !Request.Headers.TryGetValue(ApiKeyHeaderName, out oAuthHeader)
            )
            {
                // If neither accessToken, nor oAuthHeader are available we can't authenticate --> No result
                return AuthenticateResult.NoResult();
            }

            // Try to use the OAuth header first. If that is empty use the access_token 
            string providedApiKey = oAuthHeader.FirstOrDefault();
            if (string.IsNullOrEmpty(providedApiKey))
                providedApiKey = accessToken.FirstOrDefault();

            // If we got an OAuth or Bearer token but it's empty we can't authenticate --> No result
            if (string.IsNullOrWhiteSpace(providedApiKey))
                return AuthenticateResult.NoResult();

            // Try Bearer, then OAuth until one of them returns a ticket
            AuthenticationTicket ticket = WebsocketBearer(providedApiKey) ?? await OAuth(providedApiKey);

            // No ticket means we can't authenticate --> No result
            return ticket is null
                ? AuthenticateResult.NoResult()
                : AuthenticateResult.Success(ticket);
        }

        private AuthenticationTicket WebsocketBearer(string providedApiKey)
        {
            if (!Context.WebSockets.IsWebSocketRequest &&
                (Context.WebSockets.IsWebSocketRequest || !providedApiKey.StartsWith("Bearer"))) return null;

            if (!Context.WebSockets.IsWebSocketRequest && providedApiKey.StartsWith("Bearer"))
                providedApiKey = providedApiKey[7..];

            List<Claim> signalRClaims = new()
            {
                //new Claim(ClaimTypes.Name, providedApiKey),
                new Claim(ClaimTypes.NameIdentifier, providedApiKey),
            };

            ClaimsIdentity claimsIdentity = new(signalRClaims, ApiKeyAuthenticationOptions.AuthenticationType);
            List<ClaimsIdentity> claimsIdentities = new() {claimsIdentity};
            ClaimsPrincipal claimsPrincipal = new(claimsIdentities);
            return new AuthenticationTicket(claimsPrincipal, ApiKeyAuthenticationOptions.Scheme);
        }

        private async Task<AuthenticationTicket> OAuth(string providedApiKey)
        {
            if (!providedApiKey.StartsWith("OAuth")) return null;

            TwitchValidateResult validate = await TwitchOAuthHandler.Validate(providedApiKey);

            if (string.IsNullOrEmpty(validate.UserId))
            {
                //Response.StatusCode = validate.Status;
                //await Response.WriteAsync(validate.Message);
                return null;
            }

            List<Claim> claims = new()
            {
                //new Claim(ClaimTypes.Name, validate.Login),

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
            return new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.Scheme);
        }

        private void SetRoles(TwitchValidateResult validate, ICollection<Claim> claims)
        {
            if (!int.TryParse(validate.UserId, out int userId))
                return;

            // Bot check
            if (new[] {1234}.Contains(userId))
                claims.Add(new Claim(ClaimTypes.Role, Roles.Roles.ChatBot));

            // Admin check
            if (new[] {38949074}.Contains(userId))
                claims.Add(new Claim(ClaimTypes.Role, Roles.Roles.Admin));

            // Get channelId from Route and userId from validate
            if (
                !Request.RouteValues.TryGetValue("channelId", out object channelIdStr) ||
                channelIdStr == null ||
                !int.TryParse(channelIdStr.ToString(), out int channelId)
            )
                return;

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
