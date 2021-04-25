using System.IO;

namespace TtsApi.Hubs.TransferClasses
{
    public class TtsIndividualSynthesize
    {
        public byte[] VoiceData { get; set; }

        public float PlaybackRate { get; set; }

        public TtsIndividualSynthesize(Stream input, float playbackRate)
        {
            using MemoryStream ms = new();
            input.CopyTo(ms);
            VoiceData = ms.ToArray();

            PlaybackRate = playbackRate;
        }
    }
}
