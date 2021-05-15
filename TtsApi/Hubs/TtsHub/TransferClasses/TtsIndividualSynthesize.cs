using System;
using System.IO;

namespace TtsApi.Hubs.TtsHub.TransferClasses
{
    public class TtsIndividualSynthesize
    {
        public string VoiceDataWavBase64 { get; set; } = "";

        public float PlaybackRate { get; set; } = 1.0f;

        public float Volume { get; set; } = 100;

        public TtsIndividualSynthesize()
        {
        }

        public TtsIndividualSynthesize(Stream input, float playbackRate, float volume)
        {
            using MemoryStream ms = new();
            input.CopyTo(ms);
            VoiceDataWavBase64 = Convert.ToBase64String(ms.ToArray());

            PlaybackRate = playbackRate;
            Volume = volume;
        }
    }
}
