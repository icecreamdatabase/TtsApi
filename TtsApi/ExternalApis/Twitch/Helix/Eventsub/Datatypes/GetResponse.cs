﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes
{
    [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
    public class GetResponse : TwitchErrorBase
    {
        public GetResponse(Subscription<dynamic>[] data)
        {
            if (data == null) 
                return;
            Data = data;
            foreach (Subscription<dynamic> sub in data)
            {
                switch (sub.Type)
                {
                    case ConditionMap.ChannelPointsCustomRewardRedemptionAdd:
                        ChannelPointsCustomRewardRedemptionAdds.Add(
                            ParseCondition<ChannelPointsCustomRewardRedemptionAddCondition>(sub)
                        );
                        break;
                    case ConditionMap.ChannelPointsCustomRewardRedemptionUpdate:
                        ChannelPointsCustomRewardRedemptionUpdates.Add(
                            ParseCondition<ChannelPointsCustomRewardRedemptionUpdateCondition>(sub)
                        );
                        break;
                    case ConditionMap.ChannelBan:
                        ChannelBans.Add(
                            ParseCondition<ChannelBanCondition>(sub)
                        );
                        break;
                    case ConditionMap.UserAuthorizationRevoke:
                        UserAuthorizationRevokes.Add(
                            ParseCondition<UserAuthorizationRevokeCondition>(sub)
                        );
                        break;
                }
            }
        }

        [JsonIgnore]
        public List<Subscription<ChannelPointsCustomRewardRedemptionAddCondition>>
            ChannelPointsCustomRewardRedemptionAdds { get; } = new();

        [JsonIgnore]
        public List<Subscription<ChannelPointsCustomRewardRedemptionUpdateCondition>>
            ChannelPointsCustomRewardRedemptionUpdates { get; } = new();

        [JsonIgnore]
        public List<Subscription<ChannelBanCondition>> ChannelBans { get; } = new();

        [JsonIgnore]
        public List<Subscription<UserAuthorizationRevokeCondition>> UserAuthorizationRevokes { get; } = new();

        // Use something like this to parse it:
        // JsonSerializer.Deserialize<T>(((JsonElement)subscriptions.Data[0].Condition).GetRawText());
        // Check Constructor and ParseCondition<T>()
        [JsonPropertyName("data")]
        public Subscription<dynamic>[] Data { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; init; }

        [JsonPropertyName("total_cost")]
        public int TotalCost { get; init; }

        [JsonPropertyName("max_total_cost")]
        public int MaxTotalCost { get; init; }

        [JsonPropertyName("pagination")]
        public object Pagination { get; init; }

        private static Subscription<T> ParseCondition<T>(Subscription<dynamic> sub)
        {
            T parsed = JsonSerializer.Deserialize<T>(((JsonElement)sub.Condition).GetRawText());

            return new Subscription<T>
            {
                Cost = sub.Cost,
                Id = sub.Id,
                Status = sub.Status,
                Transport = sub.Transport,
                Type = sub.Type,
                Version = sub.Version,
                CreatedAt = sub.CreatedAt,
                Condition = parsed
            };
        }

        public static List<Subscription<T>> FilterByTransportData<T>(IEnumerable<Subscription<T>> subscriptions,
            Transport transport)
        {
            return subscriptions.Where(subscription => subscription.Transport == transport).ToList();
        }
    }
}
