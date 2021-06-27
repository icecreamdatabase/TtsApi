using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TtsApi.ExternalApis.Aws;

namespace TtsApi.BackgroundServices
{
    /// <summary>
    /// https://andrewlock.net/running-async-tasks-on-app-startup-in-asp-net-core-3/
    /// </summary>
    public class PrefetchPollyData : IHostedService
    {
        private readonly Polly _polly;

        public PrefetchPollyData(IServiceProvider serviceProvider)
        {
            _polly = serviceProvider.GetRequiredService<Polly>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _polly.InitVoicesData();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
