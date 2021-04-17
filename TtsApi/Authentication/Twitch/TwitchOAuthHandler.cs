using System;
using System.IO;
using System.Net;
using System.Text.Json;

namespace TtsApi.Authentication.Twitch
{
    public static class TwitchOAuthHandler
    {
        public static TwitchValidateResult Validate(string oauth)
        {
            TwitchValidateResult validateResult = new();

            WebRequest request = WebRequest.Create(@"https://id.twitch.tv/oauth2/validate");
            request.Headers.Add(HttpRequestHeader.Authorization, oauth);
            WebResponse response = request.GetResponse();
            //if (((HttpWebResponse) response).StatusCode != HttpStatusCode.OK)

            using (Stream dataStream = response.GetResponseStream())
            {
                StreamReader reader = new(dataStream ?? throw new InvalidOperationException());
                string responseFromServer = reader.ReadToEnd();
                validateResult = JsonSerializer.Deserialize<TwitchValidateResult>(responseFromServer);
            }

            // Close the response.
            response.Close();
            return validateResult;
        }
    }
}
