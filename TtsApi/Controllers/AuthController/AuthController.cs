﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Controllers.AuthController.AuthData;
using TtsApi.ExternalApis.Twitch.Helix.Auth.DataTypes;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub;
using TtsApi.ExternalApis.Twitch.Helix.Users;
using TtsApi.ExternalApis.Twitch.Helix.Users.DataTypes;
using TtsApi.Model;
using TtsApi.Model.Schema;
using static TtsApi.ExternalApis.Twitch.Helix.Auth.Authentication;

namespace TtsApi.Controllers.AuthController
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly TtsDbContext _db;
        private readonly Users _users;
        private readonly Subscriptions _subscriptions;

        private static readonly List<string> RegisterRequiredScopes = new()
            {"channel:manage:redemptions", "channel:read:redemptions", "moderation:read", "channel:moderate"};

        private static readonly string[] ValidSignupBroadcasterTypes = {"partner", "affiliate"};

        public AuthController(ILogger<AuthController> logger, TtsDbContext db, Users users, Subscriptions subscriptions)
        {
            _logger = logger;
            _db = db;
            _users = users;
            _subscriptions = subscriptions;
        }

        /// <summary>
        /// Redirects to the <c>oauth2/authorize</c> endpoint url required to login into the dashboard.
        /// The resulting code will then need to be sent to the <see cref="Register"/> endpoint.<br />
        /// <br />
        /// </summary>
        /// <returns></returns>
        /// <response code="302">Redirecting to uri</response>
        [HttpGet("Links/LoginForDashboard")]
        [ProducesResponseType((int) HttpStatusCode.Redirect)]
        public ActionResult GetLinkLogin()
        {
            return Redirect(AuthDataLogin.FullUrl);
        }
        
        /// <summary>
        /// Redirects to the <c>oauth2/authorize</c> endpoint url required to signup for tts.
        /// The resulting code will then need to be sent to the <see cref="Register"/> endpoint.<br />
        /// <br />
        /// </summary>
        /// <returns></returns>
        /// <response code="302">Redirecting to uri</response>
        [HttpGet("Links/SignupForTts")]
        [ProducesResponseType((int) HttpStatusCode.Redirect)]
        public ActionResult GetLinkSignup()
        {
            return Redirect(AuthDataSignup.FullUrl);
        }

        /// <summary>
        /// Signup as a broadcaster.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        /// <response code="204">Channel registered.</response>
        /// <response code="400">No code provided.</response>
        /// <response code="403">Invalid code or missing scopes.</response>
        /// <response code="404">Channel not found.</response>
        [HttpPost("Register")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<ActionResult> Register([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest();

            string clientId = BotDataAccess.ClientId;
            string clientSecret = BotDataAccess.ClientSecret;
            TwitchTokenResult tokenResult = await GenerateAccessToken(clientId, clientSecret, code);
            if (string.IsNullOrEmpty(tokenResult.AccessToken))
                return Forbid();

            TwitchValidateResult validateResult = await Validate(tokenResult.AccessToken);
            if (string.IsNullOrEmpty(validateResult.UserId))
                return Forbid();

            if (_db.GlobalUserBlacklist.Any(gub => gub.UserId.ToString() == validateResult.UserId))
                return Problem($"User is blacklisted", null, (int) HttpStatusCode.Forbidden);

            bool hasAllRequiredScopes = RegisterRequiredScopes
                .All(requiredScope => validateResult.Scopes
                    .Select(s => s.ToLowerInvariant())
                    .Contains(requiredScope.ToLowerInvariant())
                );
            if (!hasAllRequiredScopes)
                return Problem($"Missing scopes. Registering requires: {string.Join(", ", RegisterRequiredScopes)}",
                    null, (int) HttpStatusCode.Forbidden);

            // Check for partner / affiliate status
            TwitchUser twitchUser = await _users.GetById(validateResult.UserId);
            if (!ValidSignupBroadcasterTypes.Contains(twitchUser.BroadcasterType))
            {
                return Problem($"User is not a partner or affiliate", null, (int) HttpStatusCode.Forbidden);
            }

            Channel entity = _db.Channels.FirstOrDefault(channel => channel.RoomId == int.Parse(validateResult.UserId));

            if (entity is null)
            {
                //TODO: all the default values are being set by the object instead of the db!
                entity = new Channel
                {
                    RoomId = int.Parse(validateResult.UserId),
                    ChannelName = validateResult.Login,
                    IsTwitchPartner = twitchUser.BroadcasterType == "partner",
                    Enabled = true
                };
                _db.Channels.Add(entity);
            }
            else
            {
                if (!string.IsNullOrEmpty(entity.AccessToken))
                    await Revoke(clientId, entity.AccessToken);
            }

            entity.RefreshToken = tokenResult.RefreshToken;
            entity.AccessToken = tokenResult.AccessToken;

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
