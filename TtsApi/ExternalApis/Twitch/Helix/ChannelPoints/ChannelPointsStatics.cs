using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Datatypes;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints
{
    public static class ChannelPointsStatics
    {
        private static readonly HttpClient Client = new();
        private const string BaseUrlCustomRewards = @"https://api.twitch.tv/helix/channel_points/custom_rewards";
        private static readonly JsonSerializerOptions JsonIgnoreNullValues = new() {IgnoreNullValues = true};

        internal static async Task<DataHolder<TwitchCustomReward>> CreateCustomReward(string clientId,
            Channel targetChannel, TwitchCustomRewardInput twitchCustomRewardInput)
        {
            using HttpRequestMessage requestMessage = new();
            GetRequest(
                requestMessage,
                clientId,
                targetChannel.AccessToken,
                twitchCustomRewardInput,
                new Dictionary<string, string>
                {
                    {"broadcaster_id", targetChannel.RoomId.ToString()},
                }
            );

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<DataHolder<TwitchCustomReward>>(responseFromServer, JsonIgnoreNullValues);
        }

        private static void GetRequest(HttpRequestMessage requestMessage, string clientId, string accessToken,
            object payload, IDictionary<string, string> query)
        {
            Uri requestUri = new(QueryHelpers.AddQueryString(BaseUrlCustomRewards, query), UriKind.Absolute);
            string payloadJson = JsonSerializer.Serialize(payload, JsonIgnoreNullValues);

            requestMessage.Method = HttpMethod.Post;
            requestMessage.RequestUri = requestUri;
            requestMessage.Headers.Add("client-id", clientId);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            requestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        }
    }
}
