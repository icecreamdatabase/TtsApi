﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.DataTypes;
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
            using HttpRequestMessage requestMessage = new() {Method = HttpMethod.Post};
            GetRequest(
                requestMessage,
                clientId,
                targetChannel.AccessToken,
                new Dictionary<string, string>
                {
                    {"broadcaster_id", targetChannel.RoomId.ToString()},
                },
                twitchCustomRewardInput
            );

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<TwitchCustomReward> {Status = (int) response.StatusCode}
                : JsonSerializer.Deserialize<DataHolder<TwitchCustomReward>>(responseFromServer, JsonIgnoreNullValues);
        }

        internal static async Task<DataHolder<object>> DeleteCustomReward(string clientId,
            Reward targetReward)
        {
            using HttpRequestMessage requestMessage = new() {Method = HttpMethod.Delete};
            GetRequest(
                requestMessage,
                clientId,
                targetReward.Channel.AccessToken,
                new Dictionary<string, string>
                {
                    {"broadcaster_id", targetReward.ChannelId.ToString()},
                    {"id", targetReward.RewardId}
                }
            );

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? new DataHolder<object> {Status = (int) response.StatusCode}
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