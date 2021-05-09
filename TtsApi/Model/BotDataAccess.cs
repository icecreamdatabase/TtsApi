using System.Linq;
using Microsoft.EntityFrameworkCore;
using TtsApi.Model.Schema;

namespace TtsApi.Model
{
    public static class BotDataAccess
    {
        public static string GetClientId(DbSet<BotData> botData)
        {
            return Get(botData, "clientId");
        }

        public static string GetClientSecret(DbSet<BotData> botData)
        {
            return Get(botData, "clientSecret");
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
                botData.Add(new BotData {Key = key, Value = value});
            }
        }
    }
}
