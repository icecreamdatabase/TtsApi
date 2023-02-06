using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Polly.Model;
using TtsApi.ExternalApis.Aws;
using TtsApi.Hubs.TtsHub.TransformationClasses;

namespace TtsApi.Hubs.TtsHub.TransferClasses
{
    public class TtsIndividualSynthesize
    {
        public string VoiceDataWavBase64 { get; set; } = "";

        public List<SpeechMark>? SpeechMarks { get; set; }

        public TtsMessagePart TtsMessagePart { get; set; }

        public int RequestCharacters { get; set; }

        [Obsolete($"Use {nameof(TtsMessagePart)}.{nameof(TtsMessagePart.PlaybackSpeed)} instead")]
        public float PlaybackRate { get; set; } = 1.0f;

        [Obsolete($"Use {nameof(TtsMessagePart)}.{nameof(TtsMessagePart.Volume)} instead")]
        public float Volume { get; set; } = 100;


        public TtsIndividualSynthesize()
        {
            TtsMessagePart = new();
        }

        private TtsIndividualSynthesize(string voiceDataWavBase64, List<SpeechMark>? speechMarks,
            int requestCharacters, TtsMessagePart ttsMessagePart)
        {
            VoiceDataWavBase64 = voiceDataWavBase64;
            SpeechMarks = speechMarks;
            TtsMessagePart = ttsMessagePart;
            RequestCharacters = requestCharacters;
            PlaybackRate = ttsMessagePart.PlaybackSpeed;
            Volume = ttsMessagePart.Volume;
        }

        public static async Task<TtsIndividualSynthesize> ParseFromSynthesizeTasks(
            Task<SynthesizeSpeechResponse> synthesizeTask, Task<SynthesizeSpeechResponse>? speechMarksTask,
            TtsMessagePart ttsMessagePart)
        {
            SynthesizeSpeechResponse audio = await synthesizeTask;

            using MemoryStream ms = new();
            await audio.AudioStream.CopyToAsync(ms);
            string voiceDataWavBase64 = Convert.ToBase64String(ms.ToArray());

            List<SpeechMark>? speechMarks = null;

            if (speechMarksTask != null)
                speechMarks = await SpeechMark.ParseSpeechMarks((await speechMarksTask).AudioStream);

            return new TtsIndividualSynthesize(voiceDataWavBase64, speechMarks, audio.RequestCharacters,
                ttsMessagePart);
        }
    }
}
