using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.Authentication.Roles;
using TtsApi.ExternalApis.Twitch.Eventsub;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes;
using TtsApi.Model;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
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

        [HttpGet("{channelId}")]
        [Authorize(Policy = Policies.CanAccessQueue)]
        public async Task<ActionResult> Get([FromRoute] string channelId)
        {
            return Ok($"xD {channelId}");
        }

        [HttpGet("GetDb")]
        [Authorize(Roles = Roles.BotOwner)]
        public ActionResult GetDb()
        {
            _ttsDbContext.Database.EnsureCreated();
            return Ok(_ttsDbContext.Database.GenerateCreateScript());
        }
    }
}
