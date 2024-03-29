﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events;
using TtsApi.ExternalApis.Twitch.Helix.Moderation;
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;

namespace TtsApi.Controllers.EventSubController
{
    [ApiController]
    [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class EventSubController : ControllerBase
    {
        private readonly ILogger<EventSubController> _logger;
        private readonly TtsRequestHandler _ttsRequestHandler;
        private readonly TtsSkipHandler _ttsSkipHandler;
        private readonly ModerationBannedUsers _moderationBannedUsers;
        private static readonly List<string> AlreadyHandledMessages = new();

        public EventSubController(ILogger<EventSubController> logger, TtsRequestHandler ttsRequestHandler, TtsSkipHandler ttsSkipHandler,
            ModerationBannedUsers moderationBannedUsers)
        {
            _logger = logger;
            _ttsRequestHandler = ttsRequestHandler;
            _ttsSkipHandler = ttsSkipHandler;
            _moderationBannedUsers = moderationBannedUsers;
        }

        /// <summary>
        /// Callback endpoint for Twitch Helix EventSub.
        /// It does full verification of the request origin.
        /// There is no reason for a user or frontend to ever call it.
        /// </summary>
        /// <returns></returns>
        /// <response code="200"></response>
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [Produces("application/json", "text/plain")]
        public async Task<ActionResult> Post()
        {
            // We are not using [FromBody] because I need access to the raw json input data. 
            // We can't serialize the [FromBody] object back to a json string either.
            // We need the original order of the json attributes.
            await using MemoryStream memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            byte[] bodyAsByteArray = memoryStream.ToArray();
            string bodyAsRawString = Encoding.UTF8.GetString(bodyAsByteArray);

            // Generic EventSubInput parsing because we don't know the exact type yet
            BareEventSubInput data;
            try
            {
                data = JsonSerializer.Deserialize<BareEventSubInput>(bodyAsRawString);

                if (data == null)
                    return BadRequest();

                if (!VerifySubscription(bodyAsByteArray))
                {
                    _logger.LogWarning("Eventsub verification failed:\nHeaders: {Headers}\nBody: {Body}",
                        JsonSerializer.Serialize(new EventSubHeaders(Request.Headers)),
                        bodyAsRawString
                    );
                    return Forbid();
                }
            }
            catch
            {
                return BadRequest();
            }

            _logger.LogInformation("New Eventsub data:\nBody: {Body}", bodyAsRawString);
            HandleData(data, bodyAsRawString);

            return string.IsNullOrEmpty(data.Challenge)
                ? Ok()
                : Ok(data.Challenge);
        }

        private bool VerifySubscription(IEnumerable<byte> bodyBytes)
        {
            if (!Request.Headers.TryGetValue("Twitch-Eventsub-Message-Id", out StringValues messageId) ||
                !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out StringValues messageTimestamp) ||
                !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Signature", out StringValues messageSignature)
            )
                return false;

            // Setup hasn't ran yet and we haven't fetched the key yet.
            if (string.IsNullOrEmpty(BotDataAccess.Hmacsha256Key))
                return false;

            List<byte> hmacMessage = new();
            hmacMessage.AddRange(Encoding.Default.GetBytes(messageId));
            hmacMessage.AddRange(Encoding.Default.GetBytes(messageTimestamp));
            hmacMessage.AddRange(bodyBytes);

            //string hmacMessage = messageId + messageTimestamp + bodyAsRawString;
            HMACSHA256 hash = new HMACSHA256(Encoding.ASCII.GetBytes(BotDataAccess.Hmacsha256Key));
            byte[] signature = hash.ComputeHash(hmacMessage.ToArray());
            string expectedSignature = "sha256=" + BitConverter.ToString(signature).Replace("-", "");

            //_logger.LogInformation("------\n{1}\n{2}\n{3}\n------", hmacMessage, expectedSignature, messageSignature);

            // Check valid signature
            return string.Equals(messageSignature, expectedSignature, StringComparison.InvariantCultureIgnoreCase);
        }

        [SuppressMessage("ReSharper", "SuggestVarOrType_Elsewhere")] // Because fuck 4 line parse statements :)
        private void HandleData(BareEventSubInput data, string bodyAsRawString)
        {
            // Don't handle setup messages
            if (data.Subscription.Status != "enabled")
                return;

            try
            {
                data.EventSubHeaders = new EventSubHeaders(Request.Headers);
            }
            catch (Exception e)
            {
                return;
            }

            // If we have already handled this ID discard it. This won't help after a restart.
            if (AlreadyHandledMessages.Contains(data.EventSubHeaders.MessageId))
                return;
            AlreadyHandledMessages.Add(data.EventSubHeaders.MessageId);

            // If the queue is over 500 elements long remove the first / oldest 200 elements
            if (AlreadyHandledMessages.Count > 500)
                AlreadyHandledMessages.RemoveRange(0, 200);

            // Check valid age ... There is currently no need for it. The id should be good enough for now
            //if ((DateTime.Now - data.EventSubHeaders.MessageTimestamp).TotalMinutes < 10)
            //    return;

            // Handle Type and Version
            switch (data.Subscription.Type, data.Subscription.Version)
            {
                case (ConditionMap.ChannelPointsCustomRewardRedemptionAdd, "1"):
                {
                    var parsed =
                        ParseEventSubInput<ChannelPointsCustomRewardRedemptionAddCondition,
                            ChannelPointsCustomRewardRedemptionEvent>(data, bodyAsRawString);
                    _ttsRequestHandler.CreateNewTtsRequest(parsed);
                    break;
                }
                case (ConditionMap.ChannelPointsCustomRewardRedemptionUpdate, "1"):
                {
                    var parsed =
                        ParseEventSubInput<ChannelPointsCustomRewardRedemptionAddCondition,
                            ChannelPointsCustomRewardRedemptionEvent>(data, bodyAsRawString);
                    break;
                }
                case (ConditionMap.UserAuthorizationRevoke, "1"):
                {
                    var parsed =
                        ParseEventSubInput<UserAuthorizationRevokeCondition,
                            UserAuthorizationRevokeEvent>(data, bodyAsRawString);
                    break;
                }
                case (ConditionMap.ChannelBan, "1"):
                {
                    var parsed =
                        ParseEventSubInput<ChannelBanCondition,
                            ChannelBanEvent>(data, bodyAsRawString);
                    _moderationBannedUsers.HandleEventSubBanEvent(parsed);
                    break;
                }
            }
        }

        private static EventSubInput<TCondition, TEvent> ParseEventSubInput<TCondition, TEvent>(BareEventSubInput data,
            string bodyAsRawString)
        {
            EventSubInput<TCondition, TEvent> parsed =
                JsonSerializer.Deserialize<EventSubInput<TCondition, TEvent>>(bodyAsRawString);
            if (parsed != null)
                parsed.EventSubHeaders = data.EventSubHeaders;
            return parsed;
        }
    }
}
