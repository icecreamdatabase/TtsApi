using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon.Polly.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Roles;
using TtsApi.ExternalApis.Aws;

namespace TtsApi.Controllers.SynthesizeSpeechController
{
    [ApiController]
    [Route("[controller]")]
    public class SynthesizeSpeechController : ControllerBase
    {
        private readonly ILogger<SynthesizeSpeechController> _logger;
        private readonly Polly _polly;

        public SynthesizeSpeechController(ILogger<SynthesizeSpeechController> logger, Polly polly)
        {
            _logger = logger;
            _polly = polly;
        }

        /// <summary>
        /// Execute a TTS request.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Audio stream.</response>
        [HttpGet]
        [Authorize(Roles = Roles.BotOwner)]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [Produces("audio/mpeg", "application/json")]
        public async Task<ActionResult<Stream>> Get([FromQuery] SynthesizeSpeechInput input)
        {
            SynthesizeSpeechResponse res = await _polly.Synthesize(input.Text, input.GetVoiceId(), input.GetEngine());
            return Ok(res.AudioStream);
        }

        /// <summary>
        /// Get all available TTS Voices.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Available TTS voices.</response>
        [HttpGet("GetVoices")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [Produces("application/json")]
        public ActionResult<List<Voice>> Get()
        {
            return Ok(Polly.VoicesData);
        }
    }
}
