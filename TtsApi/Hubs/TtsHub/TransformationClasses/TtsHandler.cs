using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Aws;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions;
using TtsApi.Hubs.TtsHub.TransferClasses;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public partial class TtsHandler
    {
        public static readonly Dictionary<string, string> ConnectClients = new();
        public static readonly Dictionary<int, string> ActiveRequests = new();

        private readonly ILogger<TtsHandler> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub, ITtsHub> _hubContext;
        private readonly Polly _polly;
        private readonly CustomRewardsRedemptions _customRewardsRedemptions;

        public TtsHandler(ILogger<TtsHandler> logger, IHubContext<TtsHub, ITtsHub> hubContext, Polly polly,
            CustomRewardsRedemptions customRewardsRedemptions, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _hubContext = hubContext;
            _polly = polly;
            _customRewardsRedemptions = customRewardsRedemptions;

            // We can't give the DB through the constructor parameters.
            IServiceProvider serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
            _ttsDbContext = serviceProvider.GetService<TtsDbContext>() ??
                            throw new Exception($"Could not fetch {nameof(TtsDbContext)}");
        }

        public async Task TrySendNextTtsRequestForChannel(int roomId)
        {
            List<RequestQueueIngest> rqis = _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .Include(r => r.Reward.Channel)
                .Include(r => r.Reward.Channel.ChannelUserBlacklist)
                .Where(r => r.Reward.ChannelId == roomId)
                .OrderBy(r => r.RequestTimestamp)
                .ToList();

            // No request for channel
            if (rqis.Count == 0)
                return;

            // A request is already running for this channel
            if (ActiveRequests.ContainsKey(rqis[0].Reward.ChannelId))
                return;

            await SendTtsRequest(rqis[0], rqis.Count);
        }

        private async Task SendTtsRequest(RequestQueueIngest rqi, int queueLength)
        {
            ActiveRequests.Add(rqi.Reward.ChannelId, rqi.RedemptionId);

            if (await CheckGlobalUserBlacklist(rqi)) return;
            if (await CheckChannelUserBlacklist(rqi)) return;
            if (await CheckManualFilterList(rqi)) return;
            if (await CheckWasTimedOut(rqi)) return;
            if (await CheckSubMode(rqi)) return;

            List<string> clients = ConnectClients
                .Where(pair => pair.Value == rqi.Reward.ChannelId.ToString())
                .Select(pair => pair.Key)
                .Distinct()
                .ToList();
            if (clients.Any())
            {
                TtsRequest ttsRequest = await GetTtsRequest(rqi);

                if (ttsRequest.TtsIndividualSynthesizes.Count > 0)
                    await _hubContext.Clients.Clients(clients).TtsPlayRequest(ttsRequest);
                else
                {
                    throw new Exception($"No message parts for RequestQueueIngest id {rqi.Id}");
                }
            }
        }

        private async Task<TtsRequest> GetTtsRequest(RequestQueueIngest rqi)
        {
            TtsRequest ttsRequest = new()
            {
                RedemptionId = rqi.RedemptionId,
                MaxMessageTimeSeconds = rqi.Reward.Channel.MaxMessageTimeSeconds,
            };

            List<Task<TtsIndividualSynthesize>> tasks = TtsHandlerStatics.SplitMessage(rqi)
                .Select(part => GenerateIndividualSynthesizeTask(rqi, part))
                .ToList();

            TtsIndividualSynthesize[] ttsIndividualSynthesizes = await Task.WhenAll(tasks);

            await _ttsDbContext.SaveChangesAsync();
            ttsRequest.TtsIndividualSynthesizes = ttsIndividualSynthesizes.ToList();

            return ttsRequest;
        }

        public async Task ConfirmTtsSkipped(string contextConnectionId, string contextUserIdentifier,
            string redemptionId)
        {
            await DoneWithPlaying(int.Parse(contextUserIdentifier), redemptionId, MessageType.Skipped);
        }

        public async Task ConfirmTtsFullyPlayed(string contextConnectionId, string contextUserIdentifier,
            string redemptionId)
        {
            await DoneWithPlaying(int.Parse(contextUserIdentifier), redemptionId, MessageType.PlayedFully);
        }

        public static void ClientDisconnected(string contextConnectionId, string contextUserIdentifier)
        {
            // There is still some other client for that same channel connected. 
            if (ConnectClients.ContainsValue(contextUserIdentifier))
                return;

            int roomId = int.Parse(contextUserIdentifier);
            ActiveRequests.Remove(roomId);
        }
    }
}
