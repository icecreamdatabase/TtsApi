using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.CustomRewards.DataTypes;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.CustomRewards
{
    public class CustomRewards
    {
        private readonly ILogger<CustomRewards> _logger;
        private readonly TtsDbContext _db;

        public CustomRewards(ILogger<CustomRewards> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<DataHolder<TwitchCustomRewards>> GetCustomReward(Channel channel, Reward reward = null)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<TwitchCustomRewards> rewardData =
                await CustomRewardsStatics.GetCustomReward(clientId, channel, reward);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                return rewardData;
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", channel.RoomId, channel.ChannelName);
            await Auth.Authentication.Refresh(_db, channel);
            // Try again. If this still returns null then so be it.
            return await CustomRewardsStatics.GetCustomReward(clientId, channel, reward);
        }

        public async Task<DataHolder<TwitchCustomRewards>> CreateCustomReward(Channel channel,
            TwitchCustomRewardsInputCreate twitchCustomRewardsInputCreate)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<TwitchCustomRewards> rewardData =
                await CustomRewardsStatics.CreateCustomReward(clientId, channel, twitchCustomRewardsInputCreate);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                return rewardData;
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", channel.RoomId, channel.ChannelName);
            await Auth.Authentication.Refresh(_db, channel);
            // Try again. If this still returns null then so be it.
            return await CustomRewardsStatics.CreateCustomReward(clientId, channel, twitchCustomRewardsInputCreate);
        }

        public async Task<DataHolder<TwitchCustomRewards>> UpdateCustomReward(Reward reward,
            TwitchCustomRewardsesInputUpdate twitchCustomRewardsesInputUpdate)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<TwitchCustomRewards> rewardData =
                await CustomRewardsStatics.UpdateCustomReward(clientId, reward.Channel, reward,
                    twitchCustomRewardsesInputUpdate);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                return rewardData;
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", reward.Channel.RoomId,
                reward.Channel.ChannelName);
            await Auth.Authentication.Refresh(_db, reward.Channel);
            // Try again. If this still returns null then so be it.
            return await CustomRewardsStatics.UpdateCustomReward(clientId, reward.Channel, reward,
                twitchCustomRewardsesInputUpdate);
        }

        public async Task<bool> DeleteCustomReward(Reward reward)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<object> rewardData =
                await CustomRewardsStatics.DeleteCustomReward(clientId, reward);
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
            rewardData = await CustomRewardsStatics.DeleteCustomReward(clientId, reward);
            // Is Ok or not found --> Delete was successful.
            return rewardData is {Status: (int) HttpStatusCode.NoContent} or {Status: (int) HttpStatusCode.NotFound};
        }
    }
}
