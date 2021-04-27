using System;
using System.IO;

namespace TtsApi.Hubs.TransferClasses
{
    public class TtsIndividualSynthesize
    {
        public string VoiceDataWavBase64 { get; set; }

        public float PlaybackRate { get; set; }

        public TtsIndividualSynthesize()
        {
        }

        public TtsIndividualSynthesize(Stream input, float playbackRate)
        {
            using MemoryStream ms = new();
            input.CopyTo(ms);
            VoiceDataWavBase64 = Convert.ToBase64String(ms.ToArray());

            PlaybackRate = playbackRate;
        }
    }
}
