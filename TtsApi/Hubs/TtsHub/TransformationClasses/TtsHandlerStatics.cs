using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.Polly;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public static class TtsHandlerStatics
    {
        private static readonly Regex CheckWord = new Regex(@"(\w+)(?:\(x?(\d*\.?\d*)\))?:");

        public static IEnumerable<TtsMessagePart> SplitMessage(RequestQueueIngest rqi)
        {
            if (!rqi.Reward.IsConversation)
                return new List<TtsMessagePart>
                {
                    new()
                    {
                        Message = rqi.RawMessage,
                        Engine = Engine.Standard,
                        PlaybackSpeed = 1.0f,
                        VoiceId = GetVoiceId(rqi.Reward.VoiceId),
                        Volume = rqi.Reward.Channel.Volume
                    }
                };


            List<TtsMessagePart> messageParts = new();

            string currentMessage = "";
            string lastVoiceId = rqi.Reward.VoiceId;
            float lastPlaybackSpeed = 1.0f;
            foreach (string word in rqi.RawMessage.Split(" "))
            {
                if (!string.IsNullOrEmpty(word) && word.EndsWith(":"))
                {
                    Match match = CheckWord.Match(word);
                    if (match.Success)
                    {
                        string voiceStr = match.Groups[1].Value;

                        VoiceId voiceId = GetVoiceId(voiceStr);
                        if (voiceId is not null)
                        {
                            string playbackSpeedStr = match.Groups[2].Value;
                            if (!string.IsNullOrEmpty(currentMessage))
                                messageParts.Add(new TtsMessagePart
                                {
                                    Message = currentMessage,
                                    VoiceId = lastVoiceId,
                                    Engine = Engine.Standard,
                                    PlaybackSpeed = lastPlaybackSpeed,
                                    Volume = rqi.Reward.Channel.Volume
                                });
                            currentMessage = "";
                            lastVoiceId = voiceId;
                            lastPlaybackSpeed = float.TryParse(playbackSpeedStr, out float playbackSpeed)
                                ? playbackSpeed
                                : 1.0f;
                            continue;
                        }
                    }
                }

                currentMessage += " " + word;
            }

            messageParts.Add(new TtsMessagePart
            {
                Message = currentMessage,
                VoiceId = lastVoiceId,
                Engine = Engine.Standard,
                PlaybackSpeed = lastPlaybackSpeed,
                Volume = rqi.Reward.Channel.Volume
            });

            return messageParts;
        }

        private static VoiceId GetVoiceId(string rawVoiceId)
        {
            return typeof(VoiceId).GetFields().Any(info => info.Name == rawVoiceId)
                ? VoiceId.FindValue(rawVoiceId)
                : null;
        }

        private static Engine GetEngine(string rawEngineId)
        {
            return typeof(Engine).GetFields().Any(info => info.Name == rawEngineId)
                ? Engine.FindValue(rawEngineId)
                : null;
        }
    }
}
