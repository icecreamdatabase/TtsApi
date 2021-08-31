using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TtsApi.ExternalApis.Aws;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes;
using TtsApi.Model;

namespace TtsApi.BackgroundServices
{
    /// <summary>
    /// https://stackoverflow.com/questions/50763577/where-to-put-code-to-run-after-startup-is-completed/50771330
    /// </summary>
    public class PrepareBotAndPrefetchData : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _hostEnvironment;

        public PrepareBotAndPrefetchData(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            /* SCOPE */
            using IServiceScope serviceScope = _serviceProvider.CreateScope();
            IServiceProvider scopeServiceProvider = serviceScope.ServiceProvider;

            /* BOT DATA */
            await using TtsDbContext ttsDbContext = scopeServiceProvider.GetRequiredService<TtsDbContext>();
            BotDataAccess.Prefetch(ttsDbContext.BotData);

            /* POLLY */
            Polly polly = scopeServiceProvider.GetRequiredService<Polly>();
            await polly.InitVoicesData();

            /* EVENT SUB */
            Subscriptions subscriptions = scopeServiceProvider.GetRequiredService<Subscriptions>();
            Transport.Default = _hostEnvironment.IsDevelopment()
                ? Transport.DefaultDevelopment
                : Transport.DefaultProduction;
            await subscriptions.SetRequiredSubscriptionsForAllChannels();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
