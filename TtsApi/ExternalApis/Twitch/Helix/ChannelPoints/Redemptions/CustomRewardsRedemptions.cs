using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions.DataTypes;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions
{
    public class CustomRewardsRedemptions
    {
        private readonly ILogger<CustomRewardsRedemptions> _logger;
        private readonly TtsDbContext _db;

        public CustomRewardsRedemptions(ILogger<CustomRewardsRedemptions> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<DataHolder<TwitchCustomRewardsRedemptions>> GetCustomRewardsRedemptions(Reward reward,
            string redemptionId = null)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<TwitchCustomRewardsRedemptions> rewardData =
                await CustomRewardsRedemptionsStatics.GetCustomReward(clientId, reward, redemptionId);
            // If we don't have an Unauthorized result return it
            if (rewardData is not { Status: (int)HttpStatusCode.Unauthorized })
                return rewardData;
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", reward.Channel.RoomId,
                reward.Channel.ChannelName);
            await Auth.Authentication.Refresh(_db, reward.Channel);
            // Try again. If this still returns null then so be it.
            return await CustomRewardsRedemptionsStatics.GetCustomReward(clientId, reward, redemptionId);
        }

        public async Task<DataHolder<TwitchCustomRewardsRedemptions>> UpdateCustomReward(RequestQueueIngest targetRqi,
            TwitchCustomRewardsRedemptionsInput twitchCustomRewardsRedemptionsInput)
        {
            string clientId = BotDataAccess.ClientId;
            // Try first time
            DataHolder<TwitchCustomRewardsRedemptions> rewardData =
                await CustomRewardsRedemptionsStatics.UpdateCustomReward(clientId, targetRqi,
                    twitchCustomRewardsRedemptionsInput);
            // If we don't have an Unauthorized result return it
            if (rewardData is not { Status: (int)HttpStatusCode.Unauthorized })
                return rewardData;
            // Else refresh the oauth
            _logger.LogInformation("Refreshing auth for {RoomId} ({ChannelName})", targetRqi.Reward.Channel.RoomId,
                targetRqi.Reward.Channel.ChannelName);
            await Auth.Authentication.Refresh(_db, targetRqi.Reward.Channel);
            // Try again. If this still returns null then so be it.
            return await CustomRewardsRedemptionsStatics.UpdateCustomReward(clientId, targetRqi,
                twitchCustomRewardsRedemptionsInput);
        }
    }
}
