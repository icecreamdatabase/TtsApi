using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.Auth.DataTypes;
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

        public async Task<GetResponse> GetSubscriptions(string status = null, bool ignoreTransportEquality = false)
        {
            GetResponse getResponse = await SubscriptionsStatics.GetSubscription(_clientId, _appAccessToken, status);

            if (getResponse is { Status: (int)HttpStatusCode.Unauthorized })
            {
                string clientSecret = BotDataAccess.ClientSecret;
                _logger.LogInformation("Fetching new AppAccessToken");
                TwitchTokenResult token = await Auth.Authentication.GetAppAccessToken(_clientId, clientSecret);
                BotDataAccess.SetAppAccessToken(_db.BotData, token.AccessToken);
                await _db.SaveChangesAsync();

                getResponse = await SubscriptionsStatics.GetSubscription(_clientId, token.AccessToken, status);
            }

            if (getResponse is { Status: { } } && getResponse.Status != (int)HttpStatusCode.OK)
                throw new Exception("Unable to refresh AppAccessToken");

            if (!ignoreTransportEquality)
            {
                getResponse.Data = getResponse.Data
                    .Where(subscription => subscription.Transport == Transport.Default)
                    .ToArray();
            }

            return getResponse;
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
            GetResponse getResponse = await GetSubscriptions("enabled");
            List<string> shouldIdsFromDb = _db.Channels
                .Where(channel => channel.Enabled)
                .Select(channel => channel.RoomId.ToString()).ToList();


            GetResponse.FilterByTransportData(getResponse.UserAuthorizationRevokes, Transport.Default);

            List<Task<bool>> tasks = new();
            tasks.AddRange(
                SetChannelBased(
                    GetResponse.FilterByTransportData(
                        getResponse.ChannelPointsCustomRewardRedemptionAdds,
                        Transport.Default
                    ),
                    shouldIdsFromDb
                )
            );
            //tasks.AddRange(
            //    SetChannelBased(
            //        GetResponse.FilterByTransportData(
            //            getResponse.ChannelPointsCustomRewardRedemptionUpdates,
            //            Transport.Default
            //        ),
            //        shouldIdsFromDb
            //    )
            //);
            tasks.AddRange(
                SetChannelBased(
                    GetResponse.FilterByTransportData(
                        getResponse.ChannelBans,
                        Transport.Default
                    ),
                    shouldIdsFromDb
                )
            );

            tasks.AddRange(SetAuthorizationRevoked(
                GetResponse.FilterByTransportData(getResponse.UserAuthorizationRevokes, Transport.Default)
            ));

            bool[] results = await Task.WhenAll(tasks);
            return results.All(everythingSuccessful => everythingSuccessful);
        }

        public async Task<bool> UnsubscribeAll(bool ignoreTransportEquality = false)
        {
            GetResponse getResponse = await GetSubscriptions();

            if (getResponse == null)
                return false;

            IEnumerable<Task<bool>> unsubscribeTasks = getResponse.Data
                .Where(subscription => ignoreTransportEquality || subscription.Transport == Transport.Default)
                .Select(subscription => DeleteSubscription(subscription.Id));
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
                _logger.LogInformation("Unsubscribed from {Type} for channel {UserId}",
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
                _logger.LogInformation("Subscribed to {Type} for channel {UserId}",
                    subscriptionType, broadcasterUserId);
            }

            return changeTasks;
        }

        private IEnumerable<Task<bool>> SetAuthorizationRevoked(
            ICollection revokes)
        {
            if (revokes.Count > 0)
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
            _logger.LogInformation("Subscribed to {Condition}", ConditionMap.UserAuthorizationRevoke);
            return new[] { CreateSubscription(request) };
        }
    }
}
