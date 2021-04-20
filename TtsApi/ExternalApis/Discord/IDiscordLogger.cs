using Microsoft.Extensions.Logging;

namespace TtsApi.ExternalApis.Discord
{
    public interface IDiscordLogger
    {
        public void LogMain(LogLevel level, string message);
    }
}
