using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TtsApi.Hubs;
using TtsApi.Model;
using TtsApi.Model.Schema;

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
            Console.WriteLine("running");
            Stopwatch sw = Stopwatch.StartNew();
            TtsDbContext db = serviceProvider.GetService<TtsDbContext>();
            IHubContext<TtsHub> ttsHubContext = serviceProvider.GetService<IHubContext<TtsHub>>();
            if (db is null || ttsHubContext is null)
                return;

            IEnumerable<RequestQueueIngest> groupedRqi = db.RequestQueueIngest
                .Include(r => r.Reward)
                .Include(r => r.Reward.Channel)
                .ToList()
                .GroupBy(req => req.Reward.ChannelId)
                .Select(ingests => ingests.FirstOrDefault());

            groupedRqi
                .Where(rqi =>
                    rqi != null //&&
                    // listOfConnectedChannelIds.Contains(rqi.Reward.ChannelId) && 
                    // rqi.RewardId is already being checked
                )
                .ToList()
                .ForEach(async rqi => await TtsHub.SendTtsRequest(ttsHubContext, rqi));

            
            foreach (RequestQueueIngest requestQueueIngest in groupedRqi)
            {
            }
            
            sw.Stop();
            Console.WriteLine($"Ran in: {sw.Elapsed.TotalMilliseconds}");
        }
    }
}
