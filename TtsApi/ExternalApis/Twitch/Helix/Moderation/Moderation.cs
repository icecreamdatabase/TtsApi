using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.Moderation.DataTypes;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.Moderation
{
    public class Moderation
    {
        private readonly ILogger<Moderation> _logger;
        private readonly TtsDbContext _db;

        public Moderation(ILogger<Moderation> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<bool> IsModerator(Channel channel, int userIdToCheck)
        {
            string clientId = BotDataAccess.GetClientId(_db.BotData);
            // Try first time
            DataHolder<TwitchModerators> rewardData =
                await ModerationStatics.Moderators(clientId, channel, userIdToCheck.ToString());
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                // If more than one was return --> yes userIdToCheck is a moderator.
                return rewardData.Data is {Count: > 0};
            // Else refresh the oauth
            await Auth.Authentication.Refresh(_db, channel);
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", channel.RoomId, channel.ChannelName);
            // Try again. If this still returns null then so be it.
            rewardData = await ModerationStatics.Moderators(clientId, channel, userIdToCheck.ToString());
            // If more than one was return --> yes userIdToCheck is a moderator.
            return rewardData.Data is {Count: > 0};
        }
    }
}
