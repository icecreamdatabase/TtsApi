using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        [Produces("application/json")]
        public ActionResult Get([FromBody] EventSubInput data)
        {
            Console.WriteLine(data);
            return Ok();
        }
    }
}
