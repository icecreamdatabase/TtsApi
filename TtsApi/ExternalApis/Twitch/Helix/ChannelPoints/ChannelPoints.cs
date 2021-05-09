using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Datatypes;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints
{
    public class ChannelPoints
    {
        private readonly ILogger<ChannelPoints> _logger;
        private readonly TtsDbContext _db;

        public ChannelPoints(ILogger<ChannelPoints> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<DataHolder<TwitchCustomReward>> CreateCustomReward(Channel channel,
            TwitchCustomRewardInput twitchCustomRewardInput)
        {
            string clientId = BotDataAccess.GetClientId(_db.BotData);
            // Try first time
            DataHolder<TwitchCustomReward> rewardData =
                await ChannelPointsStatics.CreateCustomReward(clientId, channel, twitchCustomRewardInput);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                return rewardData;
            // Else refresh the oauth
            await Auth.Authentication.Refresh(_db, channel);
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", channel.RoomId, channel.ChannelName);
            // Try again. If this still returns null then so be it.
            return await ChannelPointsStatics.CreateCustomReward(clientId, channel, twitchCustomRewardInput);
        }
    }
}
