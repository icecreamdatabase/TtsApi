using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions
{
    public class ChannelBanCondition
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadCasterUserId { get; init; }
    }
}
