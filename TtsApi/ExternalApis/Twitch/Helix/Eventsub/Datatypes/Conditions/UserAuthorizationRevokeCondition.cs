using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions
{
    public class UserAuthorizationRevokeCondition
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; init; }
       
    }
}
