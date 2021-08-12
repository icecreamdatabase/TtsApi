using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes;
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

        public async Task<GetResponse> GetSubscriptions()
        {
            string clientId = BotDataAccess.GetClientId(_db.BotData);
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            return await SubscriptionsStatics.GetSubscription(clientId, appAccessToken);
        }

        public async Task CreateSubscription()
        {
            string clientId = BotDataAccess.GetClientId(_db.BotData);
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            Request request = new Request
            {
                Type = "channel.channel_points_custom_reward_redemption.add",
                Version = "1",
                Condition = new Condition
                {
                    BroadcasterUserId = "38949074"
                },
                Transport = new Transport()
                {
                    Method = "webhook",
                    Callback = "https://apitest.icdb.dev/eventsub",
                    Secert = "icecreamdatabase"
                }
            };

            await SubscriptionsStatics.CreateSubscription(clientId, appAccessToken, request);
        }

        public async Task DeleteSubscription(string id)
        {
            string clientId = BotDataAccess.GetClientId(_db.BotData);
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            await SubscriptionsStatics.DeleteSubscription(clientId, appAccessToken, id);
        }
    }
}
