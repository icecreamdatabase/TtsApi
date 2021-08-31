using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;

namespace TtsApi.BackgroundServices
{
    public class IngestQueueHandler : TimedHostedService
    {
        protected override TimeSpan Interval { get; } = TimeSpan.FromSeconds(1.0);
        protected override TimeSpan FirstRunAfter { get; } = TimeSpan.FromSeconds(1);

        public IngestQueueHandler(IServiceProvider services) : base(services)
        {
        }

        protected override async Task RunJobAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
        {
            TtsDbContext db = serviceProvider.GetService<TtsDbContext>();
            TtsHandler ttsHandler = serviceProvider.GetService<TtsHandler>();
            if (db is null || ttsHandler is null)
                return;

            IEnumerable<Task> requestTasks = db.RequestQueueIngest
                .Include(r => r.Reward)
                .Include(r => r.Reward.Channel)
                .Where(rqi =>
                    rqi != null &&
                    TtsHandler.ConnectClients.Values.Contains(rqi.Reward.ChannelId.ToString())
                    // rqi.RewardId is already being checked
                )
                .Select(rqi => rqi.Reward.ChannelId)
                .Distinct()
                .ToList()
                .Select(ttsHandler.TrySendNextTtsRequestForChannel);

            await Task.WhenAll(requestTasks);
        }
    }
}
