using System;
using System.Collections.Generic;
using TtsApi.Controllers.EventSubController;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events;

namespace TtsApi.ExternalApis.Twitch.Helix.Moderation
{
    public static class ModerationBannedUsers
    {
        private static readonly Dictionary<string, DateTime> UserLastBannedUtc = new();

        public static void HandleEventSubBanEvent(EventSubInput<ChannelBanCondition, ChannelBanEvent> input)
        {
            string key = $"{input.Event.BroadcasterUserId}_{input.Event.UserId}";
            if (UserLastBannedUtc.TryGetValue(key, out DateTime dateTime) &&
                dateTime > input.EventSubHeaders.MessageTimestamp
            )
                return;

            Console.WriteLine(key);
            UserLastBannedUtc[key] = input.EventSubHeaders.MessageTimestamp;
        }

        public static bool UserWasTimedOutSinceRedemption(string channelId, string userId,
            DateTime redemptionDateTimeUtc)
        {
            string key = $"{channelId}_{userId}";
            return UserLastBannedUtc.TryGetValue(key, out DateTime dateTime) && dateTime > redemptionDateTimeUtc;
        }
    }
}
