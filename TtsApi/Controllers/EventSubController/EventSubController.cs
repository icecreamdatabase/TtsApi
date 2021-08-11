using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Org.BouncyCastle.Ocsp;
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
            Console.WriteLine("-------------");
            //Request.EnableBuffering();
            
            // We are not using [FromBody] because I need access to the raw bytes
            await using MemoryStream memStream = new MemoryStream();
            await Request.Body.CopyToAsync(memStream);
            byte[] rawBody = memStream.ToArray();
            string bodyAsRawString = Utf8Encoding.GetString(rawBody);
            
            //Request.Body.Position = 0;
            //using StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8);
            //string rawJsonString = await reader.ReadToEndAsync();
            _logger.LogInformation(bodyAsRawString);
            EventSubInput data = JsonSerializer.Deserialize<EventSubInput>(bodyAsRawString);

            if (data == null)
                return BadRequest();

            if (!string.IsNullOrEmpty(data.Challenge))
            {
                if (VerifySubscription(bodyAsRawString))
                    return Ok(data.Challenge);
                return Forbid();
            }


            return Ok();
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

            _logger.LogInformation("------\n{1}\n{2}\n{3}\n------", hmacMessage, expectedSignature, messageSignature);

            return string.Equals(messageSignature, expectedSignature, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
