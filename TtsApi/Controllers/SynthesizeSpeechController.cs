using System.Threading.Tasks;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.ExternalApis.Aws;
using TtsApi.Model;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SynthesizeSpeechController : ControllerBase
    {
        private readonly ILogger<SynthesizeSpeechController> _logger;
        private readonly TtsDbContext _ttsDbContext;

        public SynthesizeSpeechController(ILogger<SynthesizeSpeechController> logger, TtsDbContext ttsDbContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
        }

        [HttpGet]
        [Authorize(Policy = Policies.Admin)]
        public async Task<ActionResult> Get()
        {
            SynthesizeSpeechResponse res = await Polly.Synthesize("test", VoiceId.Brian, Engine.Standard);
            return Ok(res.AudioStream);
        }
    }
}
