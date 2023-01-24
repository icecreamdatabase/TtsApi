using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.CustomRewards.DataTypes;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.CustomRewards
{
    public static class CustomRewardsStatics
    {
        private static readonly HttpClient Client = new();
        private const string BaseUrlCustomRewards = @"https://api.twitch.tv/helix/channel_points/custom_rewards";
        private static readonly JsonSerializerOptions JsonIgnoreNullValues = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull};

        internal static async Task<DataHolder<TwitchCustomRewards>> GetCustomReward(string clientId,
            Channel targetChannel, Reward targetReward = null)
        {
            using HttpRequestMessage requestMessage = new() { Method = HttpMethod.Get };

            Dictionary<string, string> query = new()
            {
                { "broadcaster_id", targetChannel.RoomId.ToString() },
                { "only_manageable_rewards", "true" }
            };
            if (targetReward is not null)
                query.Add("id", targetReward.RewardId);
            GetRequest(
                requestMessage,
                clientId,
                targetChannel.AccessToken,
                query
            );

            while (!HelixRatelimit.Bucket.TakeTicket())
                await Task.Delay(100);

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<TwitchCustomRewards> { Status = (int)response.StatusCode }
                : JsonSerializer.Deserialize<DataHolder<TwitchCustomRewards>>(responseFromServer, JsonIgnoreNullValues);
        }

        internal static async Task<DataHolder<TwitchCustomRewards>> CreateCustomReward(string clientId,
            Channel targetChannel, TwitchCustomRewardsInputCreate twitchCustomRewardsInputCreate)
        {
            using HttpRequestMessage requestMessage = new() { Method = HttpMethod.Post };
            GetRequest(
                requestMessage,
                clientId,
                targetChannel.AccessToken,
                new Dictionary<string, string>
                {
                    { "broadcaster_id", targetChannel.RoomId.ToString() },
                },
                twitchCustomRewardsInputCreate
            );

            while (!HelixRatelimit.Bucket.TakeTicket())
                await Task.Delay(100);

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<TwitchCustomRewards> { Status = (int)response.StatusCode }
                : JsonSerializer.Deserialize<DataHolder<TwitchCustomRewards>>(responseFromServer, JsonIgnoreNullValues);
        }

        internal static async Task<DataHolder<TwitchCustomRewards>> UpdateCustomReward(string clientId,
            Channel targetChannel, Reward targetReward, TwitchCustomRewardsesInputUpdate twitchCustomRewardsesInputUpdate)
        {
            using HttpRequestMessage requestMessage = new() { Method = HttpMethod.Patch };
            GetRequest(
                requestMessage,
                clientId,
                targetChannel.AccessToken,
                new Dictionary<string, string>
                {
                    { "broadcaster_id", targetChannel.RoomId.ToString() },
                    { "id", targetReward.RewardId }
                },
                twitchCustomRewardsesInputUpdate
            );

            while (!HelixRatelimit.Bucket.TakeTicket())
                await Task.Delay(100);

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<TwitchCustomRewards> { Status = (int)response.StatusCode }
                : JsonSerializer.Deserialize<DataHolder<TwitchCustomRewards>>(responseFromServer, JsonIgnoreNullValues);
        }

        internal static async Task<DataHolder<object>> DeleteCustomReward(string clientId,
            Reward targetReward)
        {
            using HttpRequestMessage requestMessage = new() { Method = HttpMethod.Delete };
            GetRequest(
                requestMessage,
                clientId,
                targetReward.Channel.AccessToken,
                new Dictionary<string, string>
                {
                    { "broadcaster_id", targetReward.ChannelId.ToString() },
                    { "id", targetReward.RewardId }
                }
            );

            while (!HelixRatelimit.Bucket.TakeTicket())
                await Task.Delay(100);

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<object> { Status = (int)response.StatusCode }
                : JsonSerializer.Deserialize<DataHolder<object>>(responseFromServer, JsonIgnoreNullValues);
        }

        private static void GetRequest(HttpRequestMessage requestMessage, string clientId, string accessToken,
            IDictionary<string, string> query, object payload = null)
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
