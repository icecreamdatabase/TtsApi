using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.DataTypes;
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

        public async Task<DataHolder<TwitchCustomReward>> GetCustomReward(Channel channel, Reward reward = null)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<TwitchCustomReward> rewardData =
                await ChannelPointsStatics.GetCustomReward(clientId, channel, reward);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                return rewardData;
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", channel.RoomId, channel.ChannelName);
            await Auth.Authentication.Refresh(_db, channel);
            // Try again. If this still returns null then so be it.
            return await ChannelPointsStatics.GetCustomReward(clientId, channel, reward);
        }

        public async Task<DataHolder<TwitchCustomReward>> CreateCustomReward(Channel channel,
            TwitchCustomRewardInputCreate twitchCustomRewardInputCreate)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<TwitchCustomReward> rewardData =
                await ChannelPointsStatics.CreateCustomReward(clientId, channel, twitchCustomRewardInputCreate);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                return rewardData;
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", channel.RoomId, channel.ChannelName);
            await Auth.Authentication.Refresh(_db, channel);
            // Try again. If this still returns null then so be it.
            return await ChannelPointsStatics.CreateCustomReward(clientId, channel, twitchCustomRewardInputCreate);
        }

        public async Task<DataHolder<TwitchCustomReward>> UpdateCustomReward(Reward reward,
            TwitchCustomRewardInputUpdate twitchCustomRewardInputUpdate)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<TwitchCustomReward> rewardData =
                await ChannelPointsStatics.UpdateCustomReward(clientId, reward.Channel, reward,
                    twitchCustomRewardInputUpdate);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                return rewardData;
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", reward.Channel.RoomId,
                reward.Channel.ChannelName);
            await Auth.Authentication.Refresh(_db, reward.Channel);
            // Try again. If this still returns null then so be it.
            return await ChannelPointsStatics.UpdateCustomReward(clientId, reward.Channel, reward,
                twitchCustomRewardInputUpdate);
        }

        public async Task<bool> DeleteCustomReward(Reward reward)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<object> rewardData =
                await ChannelPointsStatics.DeleteCustomReward(clientId, reward);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                // Is Ok or not found --> Delete was successful.
                return rewardData is {Status: (int) HttpStatusCode.NoContent} or
                    {Status: (int) HttpStatusCode.NotFound};
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", reward.Channel.RoomId,
                reward.Channel.ChannelName);
            await Auth.Authentication.Refresh(_db, reward.Channel);
            // Try again. If this still returns null then so be it.
            rewardData = await ChannelPointsStatics.DeleteCustomReward(clientId, reward);
            // Is Ok or not found --> Delete was successful.
            return rewardData is {Status: (int) HttpStatusCode.NoContent} or {Status: (int) HttpStatusCode.NotFound};
        }
    }
}
