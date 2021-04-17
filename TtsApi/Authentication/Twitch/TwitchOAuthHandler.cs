using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;

namespace TtsApi.Authentication.Twitch
{
    public static class TwitchOAuthHandler
    {
        private const int OAuthRememberTime = 120;
        private static readonly Dictionary<string, TwitchValidateResult> PreviousValidated = new();

        public static TwitchValidateResult Validate(string oauth)
        {
            if (PreviousValidated.ContainsKey(oauth))
            {
                if ((DateTime.Now - PreviousValidated[oauth].LastUpdated ).TotalSeconds < OAuthRememberTime)
                    return PreviousValidated[oauth];
            }

            string responseFromServer = "";

            try
            {
                WebRequest request = WebRequest.Create(@"https://id.twitch.tv/oauth2/validate");
                request.Headers.Add(HttpRequestHeader.Authorization, oauth);
                WebResponse response = request.GetResponse();
                using Stream dataStream = response.GetResponseStream();
                StreamReader reader = new(dataStream ?? throw new InvalidOperationException());
                responseFromServer = reader.ReadToEnd();
                response.Close();
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    using Stream dataStream = e.Response.GetResponseStream();
                    StreamReader reader = new(dataStream ?? throw new InvalidOperationException());
                    responseFromServer = reader.ReadToEnd();
                    e.Response.Close();
                }
            }

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
