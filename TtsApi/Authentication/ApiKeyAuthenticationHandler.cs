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
using TtsApi.Model.Schema;

namespace TtsApi.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        /// <summary>
        /// Will be written into <see cref="ClaimTypes"/>.<see cref="ClaimTypes.NameIdentifier"/>
        /// and is used all around the application for auth in regards to a channel
        /// </summary>
        private const string RoomIdQueryStringName = "roomId";
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
            List<Claim> claims = new();
            // get roomId query parameter. We cannot use custom headers for websockets. Using query parameters instead
            if (Context.Request.Query.TryGetValue(RoomIdQueryStringName, out StringValues roomIdStringValues))
            {
                string roomId = roomIdStringValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(roomId))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, roomId));
            }

            StringValues oAuthHeader = StringValues.Empty;
            if (
                !Context.Request.Query.TryGetValue(AccessTokenQueryStringName, out StringValues accessToken) &&
                !Request.Headers.TryGetValue(ApiKeyHeaderName, out oAuthHeader) &&
                claims.Count == 0
            )
            {
                // If neither accessToken, nor oAuthHeader are available, nor we got a roomId claim
                // we can't authenticate at all --> No result
                return AuthenticateResult.NoResult();
            }

            // Try to use the OAuth header first. If that is empty use the access_token 
            string providedApiKey = oAuthHeader.FirstOrDefault();
            if (string.IsNullOrEmpty(providedApiKey))
                providedApiKey = accessToken.FirstOrDefault();

            // If we got an OAuth or Bearer token but it's empty we can't authenticate --> No result
            if (!string.IsNullOrWhiteSpace(providedApiKey))
            {
                // Get claims based on the provided api key
                List<Claim> oAuthClaims = await CheckTwitchOAuth(providedApiKey);
                if (oAuthClaims != null)
                    claims.AddRange(oAuthClaims);
            }

            // No claims means we can't authenticate --> No result
            if (claims.Count == 0)
                return AuthenticateResult.NoResult();

            // Generate ticket based on the claims
            ClaimsIdentity identity = new(claims, ApiKeyAuthenticationOptions.AuthenticationType);
            List<ClaimsIdentity> identities = new() {identity};
            ClaimsPrincipal principal = new(identities);
            AuthenticationTicket ticket = new(principal, ApiKeyAuthenticationOptions.Scheme);

            return AuthenticateResult.Success(ticket);
        }

        private async Task<List<Claim>> CheckTwitchOAuth(string providedApiKey)
        {
            if (!providedApiKey.StartsWith("OAuth")) return null;

            TwitchValidateResult validate = await TwitchOAuthHandler.Validate(providedApiKey);

            // if the userId is missing it must mean we got an error back from twitch validate.
            if (string.IsNullOrEmpty(validate.UserId))
            {
                //Response.StatusCode = validate.Status;
                //await Response.WriteAsync(validate.Message);
                return null;
            }

            // Generate all possible claims from the TwitchValidateResult
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

            return claims;
        }

        private void SetRoles(TwitchValidateResult validate, ICollection<Claim> claims)
        {
            // userId from validate
            if (!int.TryParse(validate.UserId, out int userId))
                return;

            string claimRole = string.Empty;
            BotSpecialUser dbUser = _ttsDbContext.BotSpecialUsers.Find(userId);

            if (dbUser?.IsIrcBot ?? false)
                claimRole = Roles.Roles.IrcBot;
            else if (dbUser?.IsBotOwner ?? false)
                claimRole = Roles.Roles.BotOwner;
            else if (dbUser?.IsBotAdmin ?? false)
                claimRole = Roles.Roles.BotAdmin;

            // Get channelId from Route 
            else if (Request.RouteValues.TryGetValue("channelId", out object channelIdStr) &&
                     channelIdStr != null &&
                     int.TryParse(channelIdStr.ToString(), out int channelId)
            )
            {
                // Broadcaster check
                if (channelId == userId)
                    claimRole = Roles.Roles.ChannelBroadcaster;

                // Mod check
                // TODO: Mod check from ThreeLetterApi 
                else if (channelId == 1234)
                    claimRole = Roles.Roles.ChannelMod;
            }

            if (!string.IsNullOrEmpty(claimRole))
                claims.Add(new Claim(ClaimTypes.Role, claimRole));
        }
    }
}
