using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using TtsApi.ExternalApis.Twitch.Eventsub;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Events;
using TtsApi.Model;

namespace TtsApi.Controllers.EventSubController
{
    [ApiController]
    [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class EventSubController : ControllerBase
    {
        private readonly ILogger<EventSubController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly Subscriptions _subscriptions;
        private static readonly UTF8Encoding Utf8Encoding = new();

        public EventSubController(ILogger<EventSubController> logger, TtsDbContext ttsDbContext,
            Subscriptions subscriptions)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _subscriptions = subscriptions;
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
            using StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8);
            string bodyAsRawString = await reader.ReadToEndAsync();

            // Generic EventSubInput parsing because we don't know the exact type yet
            BareEventSubInput data;
            try
            {
                data = JsonSerializer.Deserialize<BareEventSubInput>(bodyAsRawString);

                if (data == null)
                    return BadRequest();

                if (!VerifySubscription(bodyAsRawString))
                    return Forbid();
            }
            catch
            {
                return BadRequest();
            }

            _logger.LogInformation(bodyAsRawString);
            HandleData(data, bodyAsRawString);

            return string.IsNullOrEmpty(data.Challenge)
                ? Ok()
                : Ok(data.Challenge);
        }

        private bool VerifySubscription(string bodyAsRawString)
        {
            if (!Request.Headers.TryGetValue("Twitch-Eventsub-Message-Id", out StringValues messageId) ||
                !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out StringValues messageTimestamp) ||
                !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Signature", out StringValues messageSignature)
            )
                return false;

            string hmacMessage = messageId + messageTimestamp + bodyAsRawString;
            HMACSHA256 hash = new HMACSHA256(Encoding.ASCII.GetBytes("icecreamdatabase"));
            byte[] signature = hash.ComputeHash(Encoding.ASCII.GetBytes(hmacMessage));
            string expectedSignature = "sha256=" + BitConverter.ToString(signature).Replace("-", "");

            //_logger.LogInformation("------\n{1}\n{2}\n{3}\n------", hmacMessage, expectedSignature, messageSignature);

            // Check valid signature
            if (!string.Equals(messageSignature, expectedSignature, StringComparison.InvariantCultureIgnoreCase))
                return false;

            // Check valid age
            if (!DateTime.TryParse(messageTimestamp, out DateTime messageDateTime))
                return false;
            return (DateTime.Now - messageDateTime).TotalMinutes < 10;
        }

        private static readonly List<string> AlreadyHandledMessages = new();

        [SuppressMessage("ReSharper", "SuggestVarOrType_Elsewhere")] // Because fuck 4 line parse statements :)
        private void HandleData(BareEventSubInput data, string bodyAsRawString)
        {
            // If we have already handled this ID discard it. This won't help after a restart.
            if (AlreadyHandledMessages.Contains(data.Subscription.Id))
                return;
            AlreadyHandledMessages.Add(data.Subscription.Id);
            
            // If the queue is over 500 elements long remove the first / oldest 200 elements
            if (AlreadyHandledMessages.Count > 500)
                AlreadyHandledMessages.RemoveRange(0, 200);

            // Handle Type and Version
            switch (data.Subscription.Type, data.Subscription.Version)
            {
                case ("channel.channel_points_custom_reward_redemption.add", "1"):
                {
                    var parsed = JsonSerializer
                        .Deserialize<EventSubInput<ChannelPointsCustomRewardRedemptionCondition,
                            ChannelPointsCustomRewardRedemptionEvent>>(bodyAsRawString);
                    break;
                }
                case ("channel.channel_points_custom_reward_redemption.update", "1"):
                {
                    var parsed = JsonSerializer
                        .Deserialize<EventSubInput<ChannelPointsCustomRewardRedemptionCondition,
                            ChannelPointsCustomRewardRedemptionEvent>>(bodyAsRawString);
                    break;
                }
                case ("user.authorization.revoke", "1"):
                {
                    var parsed = JsonSerializer
                        .Deserialize<EventSubInput<UserAuthorizationRevokeNotificationCondition,
                            UserAuthorizationRevokeNotificationEvent>>(bodyAsRawString);
                    break;
                }
            }
        }
    }
}
