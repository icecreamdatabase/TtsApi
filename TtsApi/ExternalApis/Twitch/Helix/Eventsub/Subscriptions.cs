using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions;
using TtsApi.Model;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub
{
    public class Subscriptions
    {
        private readonly ILogger<Subscriptions> _logger;
        private readonly TtsDbContext _db;
        private readonly string _clientId;
        private readonly string _appAccessToken;

        public Subscriptions(ILogger<Subscriptions> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
            _clientId = BotDataAccess.ClientId;
            _appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);
        }

        public Task<GetResponse> GetSubscriptions(string status = null)
        {
            return SubscriptionsStatics.GetSubscription(_clientId, _appAccessToken, status);
        }

        private Task<bool> CreateSubscription(Request request)
        {
            return SubscriptionsStatics.CreateSubscription(_clientId, _appAccessToken, request);
        }

        private Task<bool> DeleteSubscription(string id)
        {
            return SubscriptionsStatics.DeleteSubscription(_clientId, _appAccessToken, id);
        }

        public async Task<bool> SetRequiredSubscriptionsForAllChannels()
        {
            GetResponse subscriptions = await GetSubscriptions("enabled");
            List<string> shouldIdsFromDb = _db.Channels
                .Where(channel => channel.Enabled)
                .Select(channel => channel.RoomId.ToString()).ToList();

            List<Task<bool>> tasks = new();
            tasks.AddRange(SetChannelBased(subscriptions.ChannelPointsCustomRewardRedemptionAdds, shouldIdsFromDb));
            tasks.AddRange(SetChannelBased(subscriptions.ChannelPointsCustomRewardRedemptionUpdates, shouldIdsFromDb));
            tasks.AddRange(SetChannelBased(subscriptions.ChannelBans, shouldIdsFromDb));

            tasks.AddRange(SetAuthorizationRevoked(subscriptions));

            bool[] results = await Task.WhenAll(tasks);
            return results.All(everythingSuccessful => everythingSuccessful);
        }

        public async Task<bool> UnsubscribeAll()
        {
            GetResponse getResponse = await GetSubscriptions();

            if (getResponse == null)
                return false;

            IEnumerable<Task<bool>> unsubscribeTasks =
                getResponse.Data.Select(subscription => DeleteSubscription(subscription.Id));
            bool[] unsubscribeResults = await Task.WhenAll(unsubscribeTasks);
            return unsubscribeResults.All(everythingSuccessful => everythingSuccessful);
        }

        private IEnumerable<Task<bool>> SetChannelBased<T>(
            IReadOnlyCollection<Subscription<T>> subscriptions,
            IReadOnlyCollection<string> databaseBroadcasterIds
        ) where T : BroadcasterUserIdBase
        {
            string subscriptionType = ConditionMap.Map[typeof(T)];
            List<Task<bool>> changeTasks = new();

            List<string> subscribedBroadcasterIds = subscriptions
                .Select(subscription => subscription.Condition.BroadcasterUserId)
                .ToList();

            // Need to remove
            foreach (string broadcasterUserId in subscribedBroadcasterIds.Except(databaseBroadcasterIds))
            {
                string subscriptionId = subscriptions
                    .First(subscription => subscription.Condition.BroadcasterUserId ==
                                           broadcasterUserId).Id;
                changeTasks.Add(DeleteSubscription(subscriptionId));
                _logger.LogInformation("Unsubscribed from {0} for channel {1}",
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
                changeTasks.Add(CreateSubscription(request));
                _logger.LogInformation("Subscribed to {0} for channel {1}",
                    subscriptionType, broadcasterUserId);
            }

            return changeTasks;
        }

        private IEnumerable<Task<bool>> SetAuthorizationRevoked(GetResponse getResponse)
        {
            if (getResponse.UserAuthorizationRevokes.Count > 0)
                return System.Array.Empty<Task<bool>>();

            Request request = new Request
            {
                Type = ConditionMap.UserAuthorizationRevoke,
                Version = "1",
                Condition = new UserAuthorizationRevokeCondition
                {
                    ClientId = BotDataAccess.ClientId
                }
            };
            _logger.LogInformation("Subscribed to {0}", ConditionMap.UserAuthorizationRevoke);
            return new[] { CreateSubscription(request) };
        }
    }
}
