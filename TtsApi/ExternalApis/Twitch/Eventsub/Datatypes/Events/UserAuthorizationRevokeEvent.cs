using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Events
{
    public class UserAuthorizationRevokeEvent
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; init; }

        [JsonPropertyName("user_id")]
        public string UserId { get; init; }

        [JsonPropertyName("user_login")]
        public string UserLogin { get; init; }

        [JsonPropertyName("user_name")]
        public string UserName { get; init; }
    }
}
