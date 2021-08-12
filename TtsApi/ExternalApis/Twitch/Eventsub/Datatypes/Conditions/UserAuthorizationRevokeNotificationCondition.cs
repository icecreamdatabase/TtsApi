using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions
{
    public class UserAuthorizationRevokeNotificationCondition
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; init; }
    }
}
