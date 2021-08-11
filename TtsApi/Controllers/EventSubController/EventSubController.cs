using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using TtsApi.ExternalApis.Twitch.Eventsub;
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

            EventSubInput data;
            try
            {
                data = JsonSerializer.Deserialize<EventSubInput>(bodyAsRawString);

                if (data == null)
                    return BadRequest();

                if (!VerifySubscription(bodyAsRawString))
                    return Forbid();
            }
            catch
            {
                return BadRequest();
            }


            // TODO: Handle request


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
    }
}
