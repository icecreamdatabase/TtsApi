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

            return JsonSerializer.Deserialize<TwitchValidateResult>(responseFromServer);
        }
    }
}
