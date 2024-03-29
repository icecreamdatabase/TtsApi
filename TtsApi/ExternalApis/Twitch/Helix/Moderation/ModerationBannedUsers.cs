﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TtsApi.Controllers.EventSubController;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events;
using TtsApi.Hubs.TtsHub.TransformationClasses;

namespace TtsApi.ExternalApis.Twitch.Helix.Moderation
{
    public class ModerationBannedUsers
    {
        private static readonly Dictionary<string, DateTime> UserLastBannedUtc = new();

        private readonly ILogger<ModerationBannedUsers> _logger;
        private readonly TtsSkipHandler _ttsSkipHandler;

        public ModerationBannedUsers(ILogger<ModerationBannedUsers> logger, TtsSkipHandler ttsSkipHandler)
        {
            _logger = logger;
            _ttsSkipHandler = ttsSkipHandler;
        }

        public void HandleEventSubBanEvent(EventSubInput<ChannelBanCondition, ChannelBanEvent> input)
        {
            string key = $"{input.Event.BroadcasterUserId}_{input.Event.UserId}";
            if (UserLastBannedUtc.TryGetValue(key, out DateTime dateTime) &&
                dateTime > input.EventSubHeaders.MessageTimestamp
            )
                return;

            Console.WriteLine(key);
            UserLastBannedUtc[key] = input.EventSubHeaders.MessageTimestamp;
            _ttsSkipHandler.SkipAllRequestsByUserInChannel(input.Event.BroadcasterUserId, input.Event.UserId);
        }

        public static bool UserWasTimedOutSinceRedemption(string channelId, string userId,
            DateTime redemptionDateTimeUtc)
        {
            string key = $"{channelId}_{userId}";
            return UserLastBannedUtc.TryGetValue(key, out DateTime dateTime) && dateTime > redemptionDateTimeUtc;
        }
    }
}
