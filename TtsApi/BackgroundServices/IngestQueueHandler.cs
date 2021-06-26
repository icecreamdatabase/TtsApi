using System;
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
        protected override TimeSpan Interval { get; } = TimeSpan.FromSeconds(2.5);
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

            db.RequestQueueIngest
                .Include(r => r.Reward)
                .Include(r => r.Reward.Channel)
                .ToList()
                .GroupBy(req => req.Reward.ChannelId)
                .Select(ingests => ingests.FirstOrDefault())
                .Where(rqi =>
                    rqi != null &&
                    TtsHandler.ConnectClients.Values.Contains(rqi.Reward.ChannelId.ToString())
                    // rqi.RewardId is already being checked
                )
                .ToList()
                .ForEach(async rqi => await ttsHandler.SendTtsRequest(rqi));
        }
    }
}
