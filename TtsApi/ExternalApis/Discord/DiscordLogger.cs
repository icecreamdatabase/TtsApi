using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Discord.WebhookObjects;

namespace TtsApi.ExternalApis.Discord
{
    public class DiscordLogger
    {
        private const int DiscordWebhookGroupingDelay = 2000;
        private readonly ConcurrentQueue<WebhookPostContent> _messageQueue = new();

        private static DiscordLogger GetInstance { get; } = new();

        private DiscordLogger()
        {
            new Thread(ThreadRunner).Start();
        }

        public static void Log(LogLevel level, params string[] messages)
        {
        }

        public static void LogError(Exception e)
        {
            WebhookEmbeds embed = new()
            {
                Title = LogLevel.Error.ToString(),
                Timestamp = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                Description = $"`{e.GetType().FullName}: {e.Message}`",
                Color = GetLogLevelColour(LogLevel.Error),
                Footer = new WebhookFooter
                {
                    Text = nameof(TtsApi)
                }
            };
            WebhookCreateMessage create = new() {
                Embed = new List<WebhookEmbeds> {embed}
            };
            WebhookPostContent content = new()
            {
                Username = nameof(TtsApi),
                PayloadJson = JsonSerializer.Serialize(create, new JsonSerializerOptions {IgnoreNullValues = true}),
                FileContent = e.ToString()
            };
            GetInstance._messageQueue.Enqueue(content);
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

                if (_messageQueue.TryDequeue(out WebhookPostContent content))
                {
                    if (string.IsNullOrEmpty(content.FileContent))
                    {
                        DiscordWebhook.SendEmbedsWebhook(content.LogChannel, content);
                    }
                    else
                    {
                        Dictionary<string, string> files = new()
                        {
                            {"Stacktrace", content.FileContent}
                        };

                        DiscordWebhook.SendFilesWebhook(content.LogChannel, content.Username, files, content.PayloadJson);
                    }
                }
            }
        }
    }
}
