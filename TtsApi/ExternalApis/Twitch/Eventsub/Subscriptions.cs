using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions;
using TtsApi.Model;
using TtsApi.Model.Schema;

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

        public async Task<GetResponse> GetSubscriptions(string status = null)
        {
            string clientId = BotDataAccess.ClientId;
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            return await SubscriptionsStatics.GetSubscription(clientId, appAccessToken);
        }

        public async Task CreateSubscription(Request request)
        {
            string clientId = BotDataAccess.ClientId;
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);


            await SubscriptionsStatics.CreateSubscription(clientId, appAccessToken, request);
        }

        public async Task DeleteSubscription(string id)
        {
            string clientId = BotDataAccess.ClientId;
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            await SubscriptionsStatics.DeleteSubscription(clientId, appAccessToken, id);
        }

        public async Task SetRequiredSubscriptions()
        {
            GetResponse subscriptions = await GetSubscriptions("enabled");
            List<string> subscribedBroadcasterIds = subscriptions.ChannelPointsCustomRewardRedemptionAdds
                .Select(subscription => subscription.Condition.BroadcasterUserId)
                .ToList();

            List<string> databaseBroadcasterIds = _db.Channels.Select(channel => channel.RoomId.ToString()).ToList();

            databaseBroadcasterIds.Remove("38949074");

            // Need to remove
            foreach (string broadcasterUserId in subscribedBroadcasterIds.Except(databaseBroadcasterIds))
            {
                string subscriptionId = subscriptions.ChannelPointsCustomRewardRedemptionAdds
                    .First(subscription => subscription.Condition.BroadcasterUserId == broadcasterUserId).Id;
                await DeleteSubscription(subscriptionId);
                _logger.LogInformation("Unsubscribed to {0}", broadcasterUserId);
            }

            // Need to add
            foreach (string broadcasterUserId in databaseBroadcasterIds.Except(subscribedBroadcasterIds))
            {
                Request request = new Request
                {
                    Type = ConditionMap.ChannelPointsCustomRewardRedemptionAdd,
                    Version = "1",
                    Condition = new ChannelPointsCustomRewardRedemptionAddCondition
                    {
                        BroadcasterUserId = broadcasterUserId
                    }
                };
                await CreateSubscription(request);
                _logger.LogInformation("Subscribed to {0}", broadcasterUserId);
            }
        }

        public async Task UnsubscribeAll()
        {
            GetResponse subs = await GetSubscriptions("enabled");

            List<string> ids = new();
            ids.AddRange(subs.ChannelPointsCustomRewardRedemptionAdds.Select(subscription => subscription.Id));
            ids.AddRange(subs.ChannelPointsCustomRewardRedemptionUpdates.Select(subscription => subscription.Id));
            ids.AddRange(subs.UserAuthorizationRevokes.Select(subscription => subscription.Id));

            foreach (string id in ids)
                await DeleteSubscription(id);
        }

        public async Task ReenableNotificationFailuresExceeded()
        {
            GetResponse notificationFailureExceeded = await GetSubscriptions("notification_failure_exceeded");
        }
    }
}
