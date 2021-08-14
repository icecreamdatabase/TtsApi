using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TtsApi.Model;

namespace TtsApi.BackgroundServices
{
    /// <summary>
    /// https://stackoverflow.com/questions/50763577/where-to-put-code-to-run-after-startup-is-completed/50771330
    /// </summary>
    public class PrefetchStaticBotData : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public PrefetchStaticBotData(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using IServiceScope serviceScope = _serviceProvider.CreateScope();
            using TtsDbContext ttsDbContext = serviceScope.ServiceProvider.GetRequiredService<TtsDbContext>();

            BotDataAccess.Prefetch(ttsDbContext.BotData);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
