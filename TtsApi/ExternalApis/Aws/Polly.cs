using System.Threading.Tasks;
using Amazon.Polly;
using Amazon.Polly.Model;

namespace TtsApi.ExternalApis.Aws
{
    public static class Polly
    {
        private static readonly AmazonPollyClient PollyClient = new();

        public static async Task<SynthesizeSpeechResponse> Synthesize(string text, VoiceId voiceId, Engine engine = null)
        {
            SynthesizeSpeechRequest speechRequest = new()
            {
                Text = text,
                OutputFormat = OutputFormat.Mp3,
                VoiceId = voiceId,
                Engine = engine
            };
            return await PollyClient.SynthesizeSpeechAsync(speechRequest);
        }
    }
}
