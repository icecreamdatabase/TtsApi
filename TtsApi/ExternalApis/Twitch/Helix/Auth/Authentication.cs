using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using TtsApi.ExternalApis.Twitch.Helix.Auth.DataTypes;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.Auth
{
    public static class Authentication
    {
        private static readonly HttpClient Client = new();
        private const string BaseUrlToken = @"https://id.twitch.tv/oauth2/token";
        private const string BaseUrlValidate = @"https://id.twitch.tv/oauth2/validate";
        private const string BaseUrlRevoke = @"https://id.twitch.tv/oauth2/revoke";

        public static async Task<TwitchTokenResult> GenerateAccessToken(string clientId, string clientSecret,
            string code)
        {
            Dictionary<string, string> query = new()
            {
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"code", code},
                {"grant_type", "authorization_code"},
                {"redirect_uri", "http://localhost"}
            };
            string queryString = QueryHelpers.AddQueryString(BaseUrlToken, query);

            using HttpRequestMessage requestMessage = new(HttpMethod.Post, queryString);
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TwitchTokenResult>(responseFromServer);
        }

        public static async Task<TwitchValidateResult> Validate(string oauth)
        {
            if (oauth.StartsWith("OAuth "))
                oauth = oauth[6..];

            using HttpRequestMessage requestMessage = new(HttpMethod.Get, BaseUrlValidate);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("OAuth", oauth);
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TwitchValidateResult>(responseFromServer);
        }

        public static async Task<bool> Refresh(TtsDbContext db, Channel channel)
        {
            TwitchTokenResult result = await Refresh(
                BotDataAccess.GetClientId(db.BotData),
                BotDataAccess.GetClientSecret(db.BotData),
                channel.RefreshToken
            );
            if (string.IsNullOrEmpty(result.AccessToken))
                return false;

            channel.AccessToken = result.AccessToken;
            channel.RefreshToken = result.RefreshToken;
            await db.SaveChangesAsync();
            return true;
        }

        public static async Task<TwitchTokenResult> Refresh(string clientId, string clientSecret, string refreshToken)
        {
            Dictionary<string, string> query = new()
            {
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"grant_type", "refresh_token"},
                {"refresh_token", refreshToken}
            };
            string queryString = QueryHelpers.AddQueryString(BaseUrlToken, query);

            using HttpRequestMessage requestMessage = new(HttpMethod.Post, queryString);
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TwitchTokenResult>(responseFromServer);
        }

        public static async Task Revoke(string clientId, string accessToken)
        {
            Dictionary<string, string> query = new()
            {
                {"client_id", clientId},
                {"token", accessToken}
            };
            string queryString = QueryHelpers.AddQueryString(BaseUrlRevoke, query);

            using HttpRequestMessage requestMessage = new(HttpMethod.Post, queryString);
            await Client.SendAsync(requestMessage);
        }

        public static async Task<TwitchTokenResult> GetAppAccessToken(string clientId, string clientSecret)
        {
            Dictionary<string, string> query = new()
            {
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"grant_type", "client_credentials"},
                //{"scopes", ""}
            };
            
            string queryString = QueryHelpers.AddQueryString(BaseUrlToken, query);

            using HttpRequestMessage requestMessage = new(HttpMethod.Post, queryString);
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TwitchTokenResult>(responseFromServer);
        }
    }
}
