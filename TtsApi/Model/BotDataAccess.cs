using System.Linq;
using Microsoft.EntityFrameworkCore;
using TtsApi.Model.Schema;

namespace TtsApi.Model
{
    public static class BotDataAccess
    {
        public static string Hmacsha256Key { get; private set; }
        public static string ClientId { get; private set; }
        public static string ClientSecret { get; private set; }

        public static void Prefetch(DbSet<BotData> botData)
        {
            Hmacsha256Key = Get(botData, "hmacSha256Key");
            ClientId = Get(botData, "clientId");
            ClientSecret = Get(botData, "clientSecret");
        }

        public static string GetAppAccessToken(DbSet<BotData> botData)
        {
            return Get(botData, "appAccessToken");
        }

        public static void SetAppAccessToken(DbSet<BotData> botData, string appAccessToken)
        {
            Set(botData, "appAccessToken", appAccessToken);
        }

        private static string Get(IQueryable<BotData> botData, string key)
        {
            return botData.Where(data => data.Key == key).ToList().Select(data => data.Value).FirstOrDefault();
        }

        private static void Set(DbSet<BotData> botData, string key, string value)
        {
            BotData entry = botData.Where(data => data.Key == key).ToList().FirstOrDefault();
            if (entry != null)
                entry.Value = value;
            else
            {
                botData.Add(new BotData { Key = key, Value = value });
            }
        }
    }
}
