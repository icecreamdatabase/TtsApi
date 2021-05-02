using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.Authentication.Roles;
using TtsApi.ExternalApis.Aws;
using TtsApi.Hubs;
using TtsApi.Hubs.TransferClasses;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub> _hubContext;

        public TestController(ILogger<TestController> logger, TtsDbContext ttsDbContext, IHubContext<TtsHub> hubContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _hubContext = hubContext;
        }

        [HttpGet]
        [Authorize(Roles = Roles.BotAdmin)]
        public ActionResult Get()
        {
            // Triggering IndexOutOfRangeException on purpose
            int[] a = new int[1];
            return Ok(a[2]);
        }

        [HttpGet("{channelId}")]
        [Authorize(Policy = Policies.CanAccessQueue)]
        public async Task<ActionResult> Get([FromRoute] string channelId)
        {
            SynthesizeSpeechResponse synthResp1 = await Polly.Synthesize("test 1 lllll", VoiceId.Brian, Engine.Standard);
            SynthesizeSpeechResponse synthResp2 = await Polly.Synthesize("test 2 lllll", VoiceId.Brian, Engine.Standard);

            TtsRequest ttsRequest = new()
            {
                Id = "xD",
                MaxMessageTimeSeconds = 0f,
                TtsIndividualSynthesizes = new List<TtsIndividualSynthesize>
                {
                    new(synthResp1.AudioStream, 1f, 1f),
                    new(synthResp2.AudioStream, 1f, 1f),
                }
            };
            await TtsHub.SendTtsRequest(_hubContext, channelId, ttsRequest);
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
