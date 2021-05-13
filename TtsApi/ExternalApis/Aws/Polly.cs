using System.Threading.Tasks;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.Extensions.Logging;

namespace TtsApi.ExternalApis.Aws
{
    public class Polly
    {
        private readonly ILogger<Polly> _logger;
        private readonly IAmazonPolly _amazonPolly;

        public Polly(ILogger<Polly> logger, IAmazonPolly amazonPolly)
        {
            _logger = logger;
            _amazonPolly = amazonPolly;
        }

        public async Task<SynthesizeSpeechResponse> Synthesize(string text, VoiceId voiceId, Engine engine = null)
        {
            SynthesizeSpeechRequest speechRequest = new()
            {
                Text = text,
                OutputFormat = OutputFormat.Mp3,
                VoiceId = voiceId,
                Engine = engine
            };
            return await _amazonPolly.SynthesizeSpeechAsync(speechRequest);
        }
    }
}
