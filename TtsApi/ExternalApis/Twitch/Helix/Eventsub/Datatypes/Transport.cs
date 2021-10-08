using System;
using System.Text.Json.Serialization;
using TtsApi.Model;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes
{
    public class Transport
    {
        [JsonIgnore]
        public static Transport Default { get; set; }

        [JsonIgnore]
        public static readonly Transport DefaultDevelopment = new()
        {
            Method = "webhook",
            Callback = "https://ttsapitest.icdb.dev/eventsub",
            Secret = BotDataAccess.Hmacsha256Key
        };

        [JsonIgnore]
        public static readonly Transport DefaultProduction = new()
        {
            Method = "webhook",
            Callback = "https://ttsapi.icdb.dev/eventsub",
            Secret = BotDataAccess.Hmacsha256Key
        };

        [JsonPropertyName("method")]
        public string Method { get; init; }

        [JsonPropertyName("callback")]
        public string Callback { get; init; }

        [JsonPropertyName("secret")]
        public string Secret { get; init; }

        /// <summary>
        /// Use this == Operator to check for equality while ignoring the secret.
        /// </summary>
        /// <param name="lhs">Left hand side operator.</param>
        /// <param name="rhs">Right hand side operator.</param>
        /// <returns></returns>
        public static bool operator ==(Transport lhs, Transport rhs)
        {
            if (lhs is null || rhs is null)
                return false;
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Transport lhs, Transport rhs)
        {
            return !(lhs == rhs);
        }

        private bool Equals(Transport other)
        {
            return Method == other.Method && Callback == other.Callback;
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GetType() == other.GetType() && Equals((Transport)other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Method, Callback);
        }
    }
}
