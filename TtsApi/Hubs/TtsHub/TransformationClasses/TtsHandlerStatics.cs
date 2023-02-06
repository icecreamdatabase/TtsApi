using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.Polly;
using Amazon.Polly.Model;
using TtsApi.ExternalApis.Aws;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public static class TtsHandlerStatics
    {
        private static readonly Regex CheckWord = new Regex(@"(\+)?(\w+)(\+)?(?:\(x?(\d*\.?\d*)\))?(\+)?:");
        private const int RegexGroupNeuralFront = 1;
        private const int RegexGroupVoice = 2;
        private const int RegexGroupNeuralMid = 3;
        private const int RegexGroupPlaybackSpeed = 4;
        private const int RegexGroupNeuralBack = 5;

        private static readonly VoiceId FallbackVoiceId = VoiceId.Brian;
        private static readonly Engine FallbackEngine = Engine.Standard;

        public static IEnumerable<TtsMessagePart> SplitMessage(RequestQueueIngest rqi)
        {
            List<string> messageSplit = SplitMessageToClosestWordToLimit(rqi);
            if (!rqi.Reward.IsConversation)
            {
                Voice voice = rqi.Reward.Voice;
                Engine engine = GetEngine(rqi, voice);

                bool useFallback = !rqi.Reward.Channel.AllowNeuralVoices && engine == Engine.Neural;

                return new List<TtsMessagePart>
                {
                    new()
                    {
                        Message = string.Join(' ', messageSplit),
                        VoiceId = useFallback ? FallbackVoiceId : voice.Id,
                        Engine = useFallback ? FallbackEngine : engine,
                        PlaybackSpeed = rqi.Reward.DefaultPlaybackSpeed,
                        Volume = rqi.Reward.Channel.Volume
                    }
                };
            }


            List<TtsMessagePart> messageParts = new();

            string currentMessage = "";
            Voice lastVoice = rqi.Reward.Voice;
            Engine lastEngine = GetEngine(rqi, lastVoice);
            float lastPlaybackSpeed = rqi.Reward.DefaultPlaybackSpeed;
            foreach (string word in messageSplit)
            {
                if (!string.IsNullOrEmpty(word) && word.EndsWith(":"))
                {
                    Match match = CheckWord.Match(word);
                    if (match.Success)
                    {
                        string voiceStr = match.Groups[RegexGroupVoice].Value;

                        Voice? voice = GetVoice(voiceStr);
                        if (voice is not null)
                        {
                            if (!string.IsNullOrEmpty(currentMessage))
                                messageParts.Add(new TtsMessagePart
                                {
                                    Message = currentMessage,
                                    VoiceId = lastVoice.Id,
                                    Engine = lastEngine,
                                    PlaybackSpeed = lastPlaybackSpeed,
                                    Volume = rqi.Reward.Channel.Volume
                                });

                            currentMessage = "";
                            lastVoice = voice;

                            bool tryToUseNeural = rqi.Reward.Channel.AllowNeuralVoices && (
                                !string.IsNullOrEmpty(match.Groups[RegexGroupNeuralFront].Value) ||
                                !string.IsNullOrEmpty(match.Groups[RegexGroupNeuralMid].Value) ||
                                !string.IsNullOrEmpty(match.Groups[RegexGroupNeuralBack].Value)
                            );

                            // Is it even possible to use the requested engine?
                            lastEngine = tryToUseNeural switch
                            {
                                true when voice.SupportedEngines.Contains(Engine.Neural) => Engine.Neural,
                                false when voice.SupportedEngines.Contains(Engine.Standard) => Engine.Standard,
                                _ => voice.SupportedEngines.First()
                            };

                            lastPlaybackSpeed = float.TryParse(
                                match.Groups[RegexGroupPlaybackSpeed].Value,
                                out float playbackSpeed
                            )
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
                VoiceId = lastVoice.Id,
                Engine = lastEngine,
                PlaybackSpeed = lastPlaybackSpeed,
                Volume = rqi.Reward.Channel.Volume
            });

            return messageParts;
        }

        private static List<string> SplitMessageToClosestWordToLimit(RequestQueueIngest rqi,
            int wordLengthToNeverCut = 10)
        {
            string[] rawMessageSplit = rqi.RawMessage.Split(" ");
            int messageLength = 0;
            List<string> returnMessage = new();
            foreach (string part in rawMessageSplit)
            {
                if (messageLength + part.Length > rqi.Reward.Channel.MaxTtsCharactersPerRequest)
                {
                    if (part.Length >= wordLengthToNeverCut)
                        returnMessage.Add(part[..(rqi.Reward.Channel.MaxTtsCharactersPerRequest - messageLength)]);
                    break;
                }

                messageLength += part.Length + 1; // + 1 for the spaces
                returnMessage.Add(part);
            }

            return returnMessage;
        }

        private static Voice? GetVoice(string rawVoiceId)
        {
            return typeof(VoiceId).GetFields().Any(info => info.Name == rawVoiceId)
                ? Polly.VoicesData.FirstOrDefault(v => v.Id == rawVoiceId)
                : null;
        }

        private static Engine GetEngine(RequestQueueIngest rqi, Voice voice)
        {
            Engine engine = rqi.Reward.VoiceEngine;

            if (!voice.SupportedEngines.Contains(engine))
                engine = voice.SupportedEngines.Contains(Engine.Standard)
                    ? Engine.Standard
                    : Engine.Neural;
            return engine;
        }
    }
}
