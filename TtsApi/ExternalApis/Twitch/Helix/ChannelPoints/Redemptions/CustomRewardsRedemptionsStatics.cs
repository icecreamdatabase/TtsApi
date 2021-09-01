using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions.DataTypes;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions
{
    public static class CustomRewardsRedemptionsStatics
    {
        private static readonly HttpClient Client = new();

        private const string BaseUrlCustomRewardsRedemptions =
            @"https://api.twitch.tv/helix/channel_points/custom_rewards/redemptions";

        private static readonly JsonSerializerOptions JsonIgnoreNullValues = new() { IgnoreNullValues = true };

        internal static async Task<DataHolder<TwitchCustomRewardsRedemptions>> GetCustomReward(string clientId,
            Reward targetReward, string messageId = null, string status = "UNFULFILLED")
        {
            using HttpRequestMessage requestMessage = new() { Method = HttpMethod.Get };

            Dictionary<string, string> query = new()
            {
                { "broadcaster_id", targetReward.Channel.RoomId.ToString() },
                { "reward_id", targetReward.RewardId },
                { "status", status }
            };
            if (!string.IsNullOrEmpty(messageId))
                query.Add("id", messageId);
            GetRequest(
                requestMessage,
                clientId,
                targetReward.Channel.AccessToken,
                query
            );

            while (!HelixRatelimit.Bucket.TakeTicket())
                await Task.Delay(100);

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<TwitchCustomRewardsRedemptions> { Status = (int)response.StatusCode }
                : JsonSerializer.Deserialize<DataHolder<TwitchCustomRewardsRedemptions>>(responseFromServer,
                    JsonIgnoreNullValues);
        }

        internal static async Task<DataHolder<TwitchCustomRewardsRedemptions>> UpdateCustomReward(string clientId,
            RequestQueueIngest targetRqi, TwitchCustomRewardsRedemptionsInput twitchCustomRewardInput)
        {
            using HttpRequestMessage requestMessage = new() { Method = HttpMethod.Patch };
            GetRequest(
                requestMessage,
                clientId,
                targetRqi.Reward.Channel.AccessToken,
                new Dictionary<string, string>
                {
                    { "broadcaster_id", targetRqi.Reward.ChannelId.ToString() },
                    { "id", targetRqi.MessageId },
                    { "reward_id", targetRqi.RewardId }
                },
                twitchCustomRewardInput
            );

            while (!HelixRatelimit.Bucket.TakeTicket())
                await Task.Delay(100);

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<TwitchCustomRewardsRedemptions> { Status = (int)response.StatusCode }
                : JsonSerializer.Deserialize<DataHolder<TwitchCustomRewardsRedemptions>>(responseFromServer,
                    JsonIgnoreNullValues);
        }

        private static void GetRequest(HttpRequestMessage requestMessage, string clientId, string accessToken,
            IDictionary<string, string> query, object payload = null)
        {
            Uri requestUri = new(QueryHelpers.AddQueryString(BaseUrlCustomRewardsRedemptions, query), UriKind.Absolute);
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
