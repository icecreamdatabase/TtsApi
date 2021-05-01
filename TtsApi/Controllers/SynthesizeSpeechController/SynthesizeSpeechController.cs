using System.Threading.Tasks;
using Amazon.Polly.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Roles;
using TtsApi.ExternalApis.Aws;
using TtsApi.Model;

namespace TtsApi.Controllers.SynthesizeSpeechController
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
        [Authorize(Roles = Roles.BotAdmin)]
        public async Task<ActionResult> Get([FromQuery] SynthesizeSpeechInput input)
        {
            SynthesizeSpeechResponse res = await Polly.Synthesize(input.Text, input.GetVoiceId(), input.GetEngine());
            return Ok(res.AudioStream);
        }
    }
}
