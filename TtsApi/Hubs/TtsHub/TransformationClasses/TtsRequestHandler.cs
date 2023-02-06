using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TtsApi.Controllers.EventSubController;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events;
using TtsApi.Hubs.TtsHub.TransferClasses;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public class TtsRequestHandler
    {
        public static readonly Dictionary<string, string> ConnectClients = new();
        public static readonly Dictionary<int, string> ActiveRequests = new();


        private readonly ILogger<TtsRequestHandler> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub, ITtsHub> _hubContext;
        private readonly DoneWithRequest _doneWithRequest;
        private readonly CheckTtsRequest _checkTtsRequest;
        private readonly CreateTtsRequest _createTtsRequest;

        public TtsRequestHandler(ILogger<TtsRequestHandler> logger, IHubContext<TtsHub, ITtsHub> hubContext,
            IServiceScopeFactory serviceScopeFactory, DoneWithRequest doneWithRequest,
            CheckTtsRequest checkTtsRequest, CreateTtsRequest createTtsRequest)
        {
            _logger = logger;
            _hubContext = hubContext;
            _doneWithRequest = doneWithRequest;
            _checkTtsRequest = checkTtsRequest;
            _createTtsRequest = createTtsRequest;

            // We can't give the DB through the constructor parameters.
            IServiceProvider serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
            _ttsDbContext = serviceProvider.GetService<TtsDbContext>() ??
                            throw new Exception($"Could not fetch {nameof(TtsDbContext)}");
        }


        public void CreateNewTtsRequest(EventSubInput<ChannelPointsCustomRewardRedemptionAddCondition,
            ChannelPointsCustomRewardRedemptionEvent> eventSubInput)
        {
            if (!_ttsDbContext.Rewards.Any(reward => reward.RewardId == eventSubInput.Event.Reward.Id))
                return;

            _ttsDbContext.RequestQueueIngest.Add(new RequestQueueIngest(eventSubInput));
            _ttsDbContext.SaveChanges();
        }

        public async Task TrySendNextTtsRequestForChannel(int roomId)
        {
            RequestQueueIngest? rqi = await _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .Include(r => r.Reward.Channel)
                .Include(r => r.Reward.Channel.ChannelUserBlacklist)
                .Where(r => r.Reward.ChannelId == roomId)
                .OrderBy(r => r.RequestTimestamp)
                .FirstOrDefaultAsync();

            // No request for channel
            if (rqi == null)
                return;

            // A request is already running for this channel
            if (ActiveRequests.ContainsKey(rqi.Reward.ChannelId))
                return;

            await SendTtsRequest(rqi);
        }

        private async Task SendTtsRequest(RequestQueueIngest rqi)
        {
            ActiveRequests.Add(rqi.Reward.ChannelId, rqi.RedemptionId);

            if (await _checkTtsRequest.CheckGlobalUserBlacklist(rqi)) return;
            if (await _checkTtsRequest.CheckChannelUserBlacklist(rqi)) return;
            if (await _checkTtsRequest.CheckManualFilterList(rqi)) return;
            if (await _checkTtsRequest.CheckWasTimedOut(rqi)) return;
            if (await _checkTtsRequest.CheckSubMode(rqi)) return;

            List<string> clients = ConnectClients
                .Where(pair => pair.Value == rqi.Reward.ChannelId.ToString())
                .Select(pair => pair.Key)
                .Distinct()
                .ToList();
            if (clients.Any())
            {
                TtsRequest ttsRequest = await _createTtsRequest.GetTtsRequest(rqi);
                await _ttsDbContext.SaveChangesAsync();

                if (ttsRequest.TtsIndividualSynthesizes.Count > 0)
                    await _hubContext.Clients.Clients(clients).TtsPlayRequest(ttsRequest);
                else
                    throw new Exception($"No message parts for RequestQueueIngest id {rqi.Id}");
            }
        }

        public async Task ConfirmTtsSkipped(string contextConnectionId, string contextUserIdentifier,
            string redemptionId)
        {
            await _doneWithRequest.DoneWithPlaying(int.Parse(contextUserIdentifier), redemptionId,
                MessageType.Skipped);
        }

        public async Task ConfirmTtsFullyPlayed(string contextConnectionId, string contextUserIdentifier,
            string redemptionId)
        {
            await _doneWithRequest.DoneWithPlaying(int.Parse(contextUserIdentifier), redemptionId,
                MessageType.PlayedFully);
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
