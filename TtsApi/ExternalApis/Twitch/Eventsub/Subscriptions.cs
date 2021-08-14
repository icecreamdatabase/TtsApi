using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions;
using TtsApi.Model;

namespace TtsApi.ExternalApis.Twitch.Eventsub
{
    public class Subscriptions
    {
        private readonly ILogger<Subscriptions> _logger;
        private readonly TtsDbContext _db;

        public Subscriptions(ILogger<Subscriptions> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task SetRequiredSubscriptionsForAllChannels()
        {
            GetResponse subscriptions = await GetSubscriptions("enabled");
            List<string> databaseBroadcasterIds = _db.Channels
                .Where(channel => channel.Enabled)
                .Select(channel => channel.RoomId.ToString()).ToList();

            await SetChannelBased(subscriptions.ChannelPointsCustomRewardRedemptionAdds, databaseBroadcasterIds);
            await SetChannelBased(subscriptions.ChannelPointsCustomRewardRedemptionUpdates, databaseBroadcasterIds);
            await SetChannelBased(subscriptions.ChannelBans, databaseBroadcasterIds);

            await SetAuthorizationRevoked(subscriptions);
        }

        public async Task UnsubscribeAll()
        {
            GetResponse subs = await GetSubscriptions();

            List<string> ids = new();
            ids.AddRange(subs.ChannelPointsCustomRewardRedemptionAdds.Select(subscription => subscription.Id));
            ids.AddRange(subs.ChannelPointsCustomRewardRedemptionUpdates.Select(subscription => subscription.Id));
            ids.AddRange(subs.ChannelBans.Select(subscription => subscription.Id));
            ids.AddRange(subs.UserAuthorizationRevokes.Select(subscription => subscription.Id));

            foreach (string id in ids)
                await DeleteSubscription(id);
        }

        public async Task<GetResponse> GetSubscriptions(string status = null)
        {
            string clientId = BotDataAccess.ClientId;
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            return await SubscriptionsStatics.GetSubscription(clientId, appAccessToken, status);
        }

        private async Task CreateSubscription(Request request)
        {
            string clientId = BotDataAccess.ClientId;
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            await SubscriptionsStatics.CreateSubscription(clientId, appAccessToken, request);
        }

        private async Task DeleteSubscription(string id)
        {
            string clientId = BotDataAccess.ClientId;
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            await SubscriptionsStatics.DeleteSubscription(clientId, appAccessToken, id);
        }

        public async Task ReenableNotificationFailuresExceeded()
        {
            GetResponse notificationFailureExceeded = await GetSubscriptions("notification_failure_exceeded");
        }

        private async Task SetChannelBased<T>(
            IReadOnlyCollection<Subscription<T>> subscriptions,
            IReadOnlyCollection<string> databaseBroadcasterIds
        ) where T : BroadcasterUserIdBase
        {
            string subscriptionType = ConditionMap.Map[typeof(T)];

            List<string> subscribedBroadcasterIds = subscriptions
                .Select(subscription => subscription.Condition.BroadcasterUserId)
                .ToList();

            // Need to remove
            foreach (string broadcasterUserId in subscribedBroadcasterIds.Except(databaseBroadcasterIds))
            {
                string subscriptionId = subscriptions
                    .First(subscription => subscription.Condition.BroadcasterUserId ==
                                           broadcasterUserId).Id;
                await DeleteSubscription(subscriptionId);
                _logger.LogInformation("Unsubscribed to {0} for channel {1}",
                    subscriptionType, broadcasterUserId);
            }

            // Need to add
            foreach (string broadcasterUserId in databaseBroadcasterIds.Except(subscribedBroadcasterIds))
            {
                Request request = new Request
                {
                    Type = subscriptionType,
                    Version = "1",
                    Condition = new BroadcasterUserIdBase
                    {
                        BroadcasterUserId = broadcasterUserId
                    }
                };
                await CreateSubscription(request);
                _logger.LogInformation("Subscribed to {0} for channel {1}",
                    subscriptionType, broadcasterUserId);
            }
        }

        private async Task SetAuthorizationRevoked(GetResponse getResponse)
        {
            if (getResponse.UserAuthorizationRevokes.Count == 0)
            {
                Request request = new Request
                {
                    Type = ConditionMap.UserAuthorizationRevoke,
                    Version = "1",
                    Condition = new UserAuthorizationRevokeCondition
                    {
                        ClientId = BotDataAccess.ClientId
                    }
                };
                await CreateSubscription(request);
                _logger.LogInformation("Subscribed to {0}", ConditionMap.UserAuthorizationRevoke);
            }
        }
    }
}
