using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints
{
    public class ChannelPoints
    {
        private static readonly HttpClient Client = new();
        private const string BaseUrlCustomRewards = @"https://api.twitch.tv/helix/channel_points/custom_rewards";
        private static readonly JsonSerializerOptions JsonIgnoreNullValues = new() {IgnoreNullValues = true};

        private readonly ILogger<ChannelPoints> _logger;
        private readonly TtsDbContext _db;

        public ChannelPoints(ILogger<ChannelPoints> logger, TtsDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<DataHolder<TwitchCustomReward>> CreateCustomReward(string broadcasterId, Channel channel,
            TwitchCustomRewardInput twitchCustomRewardInput)
        {
            // TODO: Make this "check twice" nicer.
            // Retry once (if a refresh has happened we want to try again once)
            int retryCounter = 2;
            while (retryCounter-- > 0)
            {
                using HttpRequestMessage requestMessage = new();
                GetRequest(
                    requestMessage,
                    channel.AccessToken,
                    twitchCustomRewardInput,
                    new Dictionary<string, string>
                    {
                        {"broadcaster_id", broadcasterId},
                    }
                );

                HttpResponseMessage response = await Client.SendAsync(requestMessage);
                string responseFromServer = await response.Content.ReadAsStringAsync();

                DataHolder<TwitchCustomReward> rewardData = JsonSerializer
                    .Deserialize<DataHolder<TwitchCustomReward>>(responseFromServer, JsonIgnoreNullValues);

                if (rewardData is not {Status: (int) HttpStatusCode.Unauthorized})
                    return rewardData;
                
                await Auth.Authentication.Refresh(_db, channel);

                //else if (rewardData is not {Status: (int) HttpStatusCode.OK})
                //    throw new HttpRequestException("Channel Points are not available for the broadcaster", null,
                //        HttpStatusCode.Forbidden);
                //else if (rewardData is
                //    {Status: (int) HttpStatusCode.BadRequest, Message: "CREATE_CUSTOM_REWARD_DUPLICATE_REWARD"})
                //    throw new HttpRequestException(rewardData.Message, null, HttpStatusCode.BadRequest);
            }

            return null;
        }

        private void GetRequest(HttpRequestMessage requestMessage, string accessToken, object payload,
            IDictionary<string, string> query)
        {
            Uri requestUri = new(QueryHelpers.AddQueryString(BaseUrlCustomRewards, query), UriKind.Absolute);
            string payloadJson = JsonSerializer.Serialize(payload, JsonIgnoreNullValues);

            requestMessage.Method = HttpMethod.Post;
            requestMessage.RequestUri = requestUri;
            requestMessage.Headers.Add("client-id", BotDataAccess.GetClientId(_db.BotData));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            requestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        }
    }
}
