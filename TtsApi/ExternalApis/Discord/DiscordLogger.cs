using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Discord.WebhookObjects;

namespace TtsApi.ExternalApis.Discord
{
    public class DiscordLogger : IDiscordLogger
    {
        private const int DiscordWebhookGroupingDelay = 2000;
        private readonly Webhook _webhook;
        private readonly Thread _queueRunnerThread;
        private readonly ConcurrentQueue<WebhookEmbeds> _messageQueue = new();

        public DiscordLogger()
        {
            _webhook = new Webhook();

            _queueRunnerThread = new Thread(ThreadRunner);
            _queueRunnerThread.Start();
        }

        public void LogMain(LogLevel level, string message)
        {
            _messageQueue.Enqueue(new WebhookEmbeds
            {
                Title = level.ToString(),
                Timestamp = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                Description = $"```\n{message}\n```",
                Color = GetLogLevelColour(level),
                Footer =
                {
                    Text = nameof(TtsApi)
                }
            });
        }

        private static int GetDecimalFromHexString(string hex)
        {
            hex = hex.Replace("#", "");
            return Convert.ToInt32(hex, 16);
        }

        private static int GetLogLevelColour(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => 12648384, //#C0FFC0
                LogLevel.Debug => 8379242, //#7FDB6A
                LogLevel.Information => 15653937, //#EEDC31
                LogLevel.Warning => 14971382, //#E47200
                LogLevel.Error => 16009031, //#F44747
                LogLevel.Critical => 0, //#000000
                LogLevel.None => 16777215, //#FFFFFF
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };
        }

        private void ThreadRunner()
        {
            while (true)
            {
                Thread.Sleep(DiscordWebhookGroupingDelay);
                if (_messageQueue.IsEmpty)
                    continue;

                List<WebhookEmbeds> embedsList = new();
                while (!_messageQueue.IsEmpty && embedsList.Count <= 10)
                {
                    //TODO: custom runner object containing some sort of "to which channel to log" value
                    if (_messageQueue.TryDequeue(out WebhookEmbeds content))
                    {
                        embedsList.Add(content);
                    }
                }

                _webhook.ExecuteWebhook(new WebhookPostContent()
                {
                    Username = nameof(TtsApi),
                    PostContent = embedsList
                });
            }
        }
    }
}
