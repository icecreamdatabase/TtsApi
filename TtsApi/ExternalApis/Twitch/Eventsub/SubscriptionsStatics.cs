using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes;

namespace TtsApi.ExternalApis.Twitch.Eventsub
{
    public static class SubscriptionsStatics
    {
        private static readonly HttpClient Client = new();
        private const string BaseUrlSubscriptions = @"https://api.twitch.tv/helix/eventsub/subscriptions";
        private static readonly JsonSerializerOptions JsonIgnoreNullValues = new() { IgnoreNullValues = true };

        internal static async Task<GetResponse> GetSubscription(string clientId, string appAccessToken, string status)
        {
            Dictionary<string, StringValues> query = null;
            if (!string.IsNullOrEmpty(status))
                query = new Dictionary<string, StringValues> { { "status", status } };

            using HttpRequestMessage requestMessage = new();
            Request(
                requestMessage,
                clientId,
                appAccessToken,
                HttpMethod.Get,
                query
            );

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();

            return string.IsNullOrEmpty(responseFromServer)
                ? null
                : JsonSerializer.Deserialize<GetResponse>(responseFromServer, JsonIgnoreNullValues);
        }

        internal static async Task<bool> CreateSubscription(string clientId, string appAccessToken, Request request)
        {
            using HttpRequestMessage requestMessage = new();
            Request(
                requestMessage,
                clientId,
                appAccessToken,
                HttpMethod.Post,
                null,
                request
            );

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            return response.IsSuccessStatusCode;
        }

        public static async Task<bool> DeleteSubscription(string clientId, string appAccessToken, string id)
        {
            using HttpRequestMessage requestMessage = new();
            Request(
                requestMessage,
                clientId,
                appAccessToken,
                HttpMethod.Delete,
                new Dictionary<string, StringValues>
                {
                    { "id", id }
                }
            );

            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            return response.IsSuccessStatusCode;
        }


        private static void Request(HttpRequestMessage requestMessage, string clientId, string appToken,
            HttpMethod httpMethod, IDictionary<string, StringValues> query = null, object payload = null)
        {
            query ??= new Dictionary<string, StringValues>();
            Uri requestUri = new(QueryHelpers.AddQueryString(BaseUrlSubscriptions, query), UriKind.Absolute);
            string payloadJson = null;
            if (payload is not null)
                payloadJson = JsonSerializer.Serialize(payload, JsonIgnoreNullValues);

            requestMessage.Method = httpMethod;
            requestMessage.RequestUri = requestUri;
            requestMessage.Headers.Add("client-id", clientId);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
            if (payloadJson is not null)
                requestMessage.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        }
    }
}
