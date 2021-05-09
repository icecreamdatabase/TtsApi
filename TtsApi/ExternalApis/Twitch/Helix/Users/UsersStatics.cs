using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using TtsApi.ExternalApis.Twitch.Helix.Users.DataTypes;

namespace TtsApi.ExternalApis.Twitch.Helix.Users
{
    public static class UsersStatics
    {
        private static readonly HttpClient Client = new();
        private const string BaseUrlCustomRewards = @"https://api.twitch.tv/helix/users";
        private static readonly JsonSerializerOptions JsonIgnoreNullValues = new() {IgnoreNullValues = true};

        internal static async Task<DataHolder<TwitchUser>> Users(string clientId, string accessToken,
            StringValues idsToCheck = new(), StringValues loginsToCheck = new())
        {
            using HttpRequestMessage requestMessage = new() {Method = HttpMethod.Get};

            switch (idsToCheck.Count + loginsToCheck.Count)
            {
                case 0:
                    return new DataHolder<TwitchUser>();
                case > 100:
                    throw new NotImplementedException("Limit of 100 Users at once at the moment."); //TODO
            }

            Dictionary<string, StringValues> query = new();
            if (idsToCheck.Count > 0)
                query.Add("id", idsToCheck);
            if (loginsToCheck.Count > 0)
                query.Add("login", loginsToCheck);

            GetRequest(
                requestMessage,
                clientId,
                accessToken,
                query
            );

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<TwitchUser> {Status = (int) response.StatusCode}
                : JsonSerializer.Deserialize<DataHolder<TwitchUser>>(responseFromServer, JsonIgnoreNullValues);
        }

        private static void GetRequest(HttpRequestMessage requestMessage, string clientId, string accessToken,
            IDictionary<string, StringValues> query, object payload = null)
        {
            Uri requestUri = new(QueryHelpers.AddQueryString(BaseUrlCustomRewards, query), UriKind.Absolute);
            string payloadJson = null;
            if (payload is not null)
                payloadJson = JsonSerializer.Serialize(payload, JsonIgnoreNullValues);

            requestMessage.Method = HttpMethod.Get;
            requestMessage.RequestUri = requestUri;
            requestMessage.Headers.Add("client-id", clientId);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (payloadJson is not null)
                requestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        }
    }
}
