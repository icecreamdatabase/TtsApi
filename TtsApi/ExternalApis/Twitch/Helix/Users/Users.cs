using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.Auth.DataTypes;
using TtsApi.ExternalApis.Twitch.Helix.Users.DataTypes;
using TtsApi.Model;

namespace TtsApi.ExternalApis.Twitch.Helix.Users
{
    public class Users
    {
        private readonly ILogger<Users> _logger;
        private readonly TtsDbContext _db;

        public Users(ILogger<Users> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
        }


        public async Task<TwitchUser> GetById(string userId)
        {
            return (await GetList(userIdsToCheck: new[] {userId})).FirstOrDefault();
        }

        public async Task<TwitchUser> GetById(int userId)
        {
            return (await GetList(userIdsToCheck: new[] {userId.ToString()})).FirstOrDefault();
        }

        public async Task<TwitchUser> GetByLogin(string login)
        {
            return (await GetList(userLoginsToCheck: new[] {login})).FirstOrDefault();
        }

        public async Task<List<TwitchUser>> GetList(string[] userIdsToCheck = null, string[] userLoginsToCheck = null)
        {
            string clientId = BotDataAccess.GetClientId(_db.BotData);
            string appAccessToken = BotDataAccess.GetAppAccessToken(_db.BotData);

            // Try first time
            DataHolder<TwitchUser> rewardData =
                await UsersStatics.Users(clientId, appAccessToken, userIdsToCheck, userLoginsToCheck);
            // If we don't have an Unauthorized result return it
            if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                return rewardData.Data;

            // Else refresh the oauth
            string clientSecret = BotDataAccess.GetClientSecret(_db.BotData);
            _logger.LogInformation("Fetching new AppAccessToken");
            TwitchTokenResult token = await Auth.Authentication.GetAppAccessToken(clientId, clientSecret);
            BotDataAccess.SetAppAccessToken(_db.BotData, token.AccessToken);
            await _db.SaveChangesAsync();
            // Try again. If this still returns null then so be it.
            rewardData = await UsersStatics.Users(clientId, token.AccessToken, userIdsToCheck, userLoginsToCheck);
            return rewardData.Data;
        }
    }
}
