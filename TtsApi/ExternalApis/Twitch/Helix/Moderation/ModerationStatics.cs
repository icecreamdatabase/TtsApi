using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using TtsApi.ExternalApis.Twitch.Helix.Moderation.DataTypes;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.Moderation
{
    public static class ModerationStatics
    {
        private static readonly HttpClient Client = new();
        private const string BaseUrlCustomRewards = @"https://api.twitch.tv/helix/moderation/moderators";
        private static readonly JsonSerializerOptions JsonIgnoreNullValues = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull};

        internal static async Task<DataHolder<TwitchModerators>> Moderators(string clientId,
            Channel targetChannel, params string[] userIdsToCheck)
        {
            using HttpRequestMessage requestMessage = new() { Method = HttpMethod.Get };
            GetRequest(
                requestMessage,
                clientId,
                targetChannel.AccessToken,
                new Dictionary<string, StringValues>
                {
                    { "broadcaster_id", targetChannel.RoomId.ToString() },
                    { "user_id", new StringValues(userIdsToCheck) },
                }
            );

            while (!HelixRatelimit.Bucket.TakeTicket())
                await Task.Delay(100);

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<TwitchModerators> { Status = (int)response.StatusCode }
                : JsonSerializer.Deserialize<DataHolder<TwitchModerators>>(responseFromServer, JsonIgnoreNullValues);
        }

        private static void GetRequest(HttpRequestMessage requestMessage, string clientId, string accessToken,
            IDictionary<string, StringValues> query, object payload = null)
        {
            Uri requestUri = new(QueryHelpers.AddQueryString(BaseUrlCustomRewards, query), UriKind.Absolute);
            string payloadJson = null;
            if (payload is not null)
                payloadJson = JsonSerializer.Serialize(payload, JsonIgnoreNullValues);

            //requestMessage.Method = HttpMethod.Post;
            requestMessage.RequestUri = requestUri;
            requestMessage.Headers.Add("client-id", clientId);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (payloadJson is not null)
                requestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        }
    }
}
