using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.Auth.DataTypes;
using TtsApi.ExternalApis.Twitch.Helix.Users;
using TtsApi.Model;
using TtsApi.Model.Schema;
using static TtsApi.ExternalApis.Twitch.Helix.Auth.Authentication;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly TtsDbContext _db;
        private readonly Users _users;

        private static readonly List<string> RegisterRequiredScopes = new()
            {"channel:manage:redemptions", "channel:read:redemptions", "moderation:read"};

        public AuthController(ILogger<AuthController> logger, TtsDbContext db, Users users)
        {
            _logger = logger;
            _db = db;
            _users = users;
        }

        [HttpPost("Register")]
        public async Task<ActionResult> Register([FromQuery] string code)
        {
            string clientId = BotDataAccess.GetClientId(_db.BotData);
            string clientSecret = BotDataAccess.GetClientSecret(_db.BotData);
            TwitchTokenResult tokenResult = await GenerateAccessToken(clientId, clientSecret, code);
            if (string.IsNullOrEmpty(tokenResult.AccessToken))
                return Forbid();

            TwitchValidateResult validateResult = await Validate(tokenResult.AccessToken);
            if (string.IsNullOrEmpty(validateResult.UserId))
                return Forbid();

            bool hasAllRequiredScopes = RegisterRequiredScopes
                .All(requiredScope => validateResult.Scopes
                    .Select(s => s.ToLowerInvariant())
                    .Contains(requiredScope.ToLowerInvariant())
                );
            if (!hasAllRequiredScopes)
                return Problem($"Missing scopes. Registering requires: {string.Join(", ", RegisterRequiredScopes)}",
                    null, (int) HttpStatusCode.Forbidden);
            
            
            //TODO: Check if Affiliate or Partner

            Channel entity = _db.Channels.FirstOrDefault(channel => channel.RoomId == int.Parse(validateResult.UserId));

            if (entity is null)
            {
                //TODO: all the default values are being set by the object instead of the db!
                entity = new Channel
                {
                    RoomId = int.Parse(validateResult.UserId),
                    ChannelName = validateResult.Login,
                    IsTwitchPartner = false,
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
