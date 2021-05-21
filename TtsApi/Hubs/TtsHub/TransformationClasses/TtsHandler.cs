using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Aws;
using TtsApi.Hubs.TtsHub.TransferClasses;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public class TtsHandler
    {
        public static readonly Dictionary<string, string> ConnectClients = new();
        private static readonly Dictionary<int, string> ActiveRequests = new();
        private readonly ILogger<TtsHandler> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub, ITtsHub> _hubContext;
        private readonly Polly _polly;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TtsHandler(ILogger<TtsHandler> logger, TtsDbContext ttsDbContext,
            IHubContext<TtsHub, ITtsHub> hubContext, Polly polly, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _hubContext = hubContext;
            _polly = polly;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task SendTtsRequest(RequestQueueIngest rqi)
        {
            if (ActiveRequests.ContainsKey(rqi.Reward.ChannelId))
                return;
            
            ActiveRequests.Add(rqi.Reward.ChannelId, rqi.Id.ToString());

            if (rqi.WasTimedOut)
            {
                await DoneWithPlaying(rqi.Reward.ChannelId, rqi.Id.ToString(), MessageType.NotPlayedTimedOut);
                return;
            }

            if (rqi.Reward.IsSubOnly && !rqi.IsSubOrHigher)
            {
                await DoneWithPlaying(rqi.Reward.ChannelId, rqi.Id.ToString(), MessageType.NotPlayedSubOnly);
                return;
            }

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
                    throw new Exception($"No message parts for reward id {rqi.Id}");
                }
            }
        }

        private async Task<TtsRequest> GetTtsRequest(RequestQueueIngest rqi)
        {
            TtsRequest ttsRequest = new()
            {
                Id = rqi.Id.ToString(),
                MaxMessageTimeSeconds = rqi.Reward.Channel.MaxMessageTimeSeconds,
            };

            List<TtsIndividualSynthesize> ttsIndividualSynthesizes = new();

            IEnumerable<TtsMessagePart> messageParts = TtsHandlerStatics.SplitMessage(rqi);
            foreach (TtsMessagePart part in messageParts.Where(part => part != null))
            {
                try
                {
                    SynthesizeSpeechResponse r = await _polly.Synthesize(part.Message, part.VoiceId, part.Engine);
                    TtsIndividualSynthesize tis = new(r.AudioStream, part.PlaybackSpeed, part.Volume);
                    ttsIndividualSynthesizes.Add(tis);
                }
                catch (AmazonPollyException e)
                {
                    _logger.LogWarning("GetTtsRequest error: {Message}", e.Message);
                    ttsIndividualSynthesizes.Add(new TtsIndividualSynthesize());
                }
            }

            ttsRequest.TtsIndividualSynthesizes = ttsIndividualSynthesizes;

            return ttsRequest;
        }

        public async Task ConfirmTtsSkipped(string contextConnectionId, string contextUserIdentifier, string id)
        {
            await DoneWithPlaying(int.Parse(contextUserIdentifier), id, MessageType.Skipped);
        }

        public async Task ConfirmTtsFullyPlayed(string contextConnectionId, string contextUserIdentifier, string id)
        {
            await DoneWithPlaying(int.Parse(contextUserIdentifier), id, MessageType.PlayedFully);
        }

        private async Task DoneWithPlaying(int roomId, string id, MessageType reason)
        {
            if (ActiveRequests.ContainsKey(roomId) && ActiveRequests[roomId] == id)
            {
                ActiveRequests.Remove(roomId);
                RequestQueueIngest rqi = await _ttsDbContext.RequestQueueIngest
                    .Include(r => r.Reward)
                    .FirstOrDefaultAsync(r => r.Id == int.Parse(id));

                //TODO: this shouldn't happen. This stuff runs in parallel. We need to lock the lockfile Pepega
                if (rqi is null)
                    return;

                _ttsDbContext.TtsLogMessages.Add(new TtsLogMessage
                {
                    RewardId = rqi.RewardId,
                    RoomId = rqi.Reward.ChannelId,
                    RequesterId = rqi.RequesterId,
                    IsSubOrHigher = rqi.IsSubOrHigher,
                    RawMessage = rqi.RawMessage,
                    VoicesId = rqi.Reward.VoiceId,
                    WasTimedOut = rqi.WasTimedOut,
                    MessageType = reason,
                    RequestTimestamp = rqi.RequestTimestamp,
                    MessageId = rqi.MessageId
                });

                _ttsDbContext.RequestQueueIngest.Remove(rqi);
                await _ttsDbContext.SaveChangesAsync();
            }
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
