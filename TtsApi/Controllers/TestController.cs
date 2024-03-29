﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.Authentication.Roles;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes;
using TtsApi.Model;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = Roles.BotOwner)]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly Subscriptions _subscriptions;

        public TestController(ILogger<TestController> logger, TtsDbContext ttsDbContext, Subscriptions subscriptions)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _subscriptions = subscriptions;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            GetResponse subscriptions = await _subscriptions.GetSubscriptions();
            return Ok(subscriptions);
        }

        [HttpPost]
        public async Task<ActionResult> Post()
        {
            await _subscriptions.SetRequiredSubscriptionsForAllChannels();
            return NoContent();
        }

        [HttpDelete]
        public async Task<ActionResult> Delete()
        {
            await _subscriptions.UnsubscribeAll();
            return NoContent();
        }
    }
}
