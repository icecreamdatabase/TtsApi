using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.Auth;
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

        public AuthController(ILogger<AuthController> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
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

            Channel entity = _db.Channels.FirstOrDefault(channel => channel.RoomId == int.Parse(validateResult.UserId));

            if (entity is null)
            {
                entity = new Channel
                {
                    RoomId = int.Parse(validateResult.UserId),
                    ChannelName = validateResult.Login,
                    IsTwitchPartner = false
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
