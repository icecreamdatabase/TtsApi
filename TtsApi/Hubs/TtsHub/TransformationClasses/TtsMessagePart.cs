using Amazon.Polly;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public class TtsMessagePart
    {
        public string Message { get; set; }

        public VoiceId VoiceId { get; set; }
        public Engine Engine { get; set; }
        public float PlaybackSpeed { get; set; }

        public float Volume { get; set; }
    }
}
