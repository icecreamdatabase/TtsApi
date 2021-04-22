using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace TtsApi.Authentication.Twitch
{
    public static class TwitchOAuthHandler
    {
        private const int OAuthRememberTime = 120;
        private static readonly Dictionary<string, TwitchValidateResult> PreviousValidated = new();
        private static readonly HttpClient Client = new();

        public static async Task<TwitchValidateResult> Validate(string oauth)
        {
            if (oauth.StartsWith("OAuth "))
                oauth = oauth[6..];

            if (PreviousValidated.ContainsKey(oauth))
            {
                if ((DateTime.Now - PreviousValidated[oauth].LastUpdated).TotalSeconds < OAuthRememberTime)
                    return PreviousValidated[oauth];
            }

            using HttpRequestMessage requestMessage = new(HttpMethod.Get, @"https://id.twitch.tv/oauth2/validate");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("OAuth", oauth);
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseFromServer = await response.Content.ReadAsStringAsync();


            TwitchValidateResult current = JsonSerializer.Deserialize<TwitchValidateResult>(responseFromServer);
            if (current != null && (!string.IsNullOrEmpty(current.Login) || !string.IsNullOrEmpty(current.Message)))
            {
                current.LastUpdated = DateTime.Now;
                PreviousValidated.Remove(oauth);
                PreviousValidated.Add(oauth, current);
            }

            return current;
        }
    }
}
