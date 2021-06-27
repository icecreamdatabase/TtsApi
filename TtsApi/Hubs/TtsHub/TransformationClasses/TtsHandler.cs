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

        public TtsHandler(ILogger<TtsHandler> logger, IHubContext<TtsHub, ITtsHub> hubContext, Polly polly,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _hubContext = hubContext;
            _polly = polly;

            // We can't give the DB through the constructor parameters.
            IServiceProvider serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
            _ttsDbContext = serviceProvider.GetService<TtsDbContext>();
        }

        public async Task SendTtsRequest(RequestQueueIngest rqiNoRef)
        {
            if (ActiveRequests.ContainsKey(rqiNoRef.Reward.ChannelId))
                return;

            ActiveRequests.Add(rqiNoRef.Reward.ChannelId, rqiNoRef.Id.ToString());

            if (rqiNoRef.WasTimedOut)
            {
                await DoneWithPlaying(rqiNoRef.Reward.ChannelId, rqiNoRef.Id.ToString(), MessageType.NotPlayedTimedOut);
                return;
            }

            if (rqiNoRef.Reward.IsSubOnly && !rqiNoRef.IsSubOrHigher)
            {
                await DoneWithPlaying(rqiNoRef.Reward.ChannelId, rqiNoRef.Id.ToString(), MessageType.NotPlayedSubOnly);
                return;
            }

            List<string> clients = ConnectClients
                .Where(pair => pair.Value == rqiNoRef.Reward.ChannelId.ToString())
                .Select(pair => pair.Key)
                .Distinct()
                .ToList();
            if (clients.Any())
            {
                TtsRequest ttsRequest = await GetTtsRequest(rqiNoRef);

                if (ttsRequest.TtsIndividualSynthesizes.Count > 0)
                    await _hubContext.Clients.Clients(clients).TtsPlayRequest(ttsRequest);
                else
                {
                    throw new Exception($"No message parts for reward id {rqiNoRef.Id}");
                }
            }
        }

        private async Task<TtsRequest> GetTtsRequest(RequestQueueIngest rqiNoRef)
        {
            TtsRequest ttsRequest = new()
            {
                Id = rqiNoRef.Id.ToString(),
                MaxMessageTimeSeconds = rqiNoRef.Reward.Channel.MaxMessageTimeSeconds,
            };

            List<TtsIndividualSynthesize> ttsIndividualSynthesizes = new();

            int characterCostStandard = 0;
            int characterCostNeural = 0;

            IEnumerable<TtsMessagePart> messageParts = TtsHandlerStatics.SplitMessage(rqiNoRef);
            foreach (TtsMessagePart part in messageParts.Where(part => part != null))
            {
                try
                {
                    SynthesizeSpeechResponse r = await _polly.Synthesize(part.Message, part.VoiceId, part.Engine);
                    if (part.Engine == Engine.Standard)
                        characterCostStandard += r.RequestCharacters;
                    else
                        characterCostNeural += r.RequestCharacters;
                    TtsIndividualSynthesize tis = new(r.AudioStream, part.PlaybackSpeed, part.Volume);
                    ttsIndividualSynthesizes.Add(tis);
                }
                catch (AmazonPollyException e)
                {
                    _logger.LogWarning("GetTtsRequest error: {Message}", e.Message);
                    ttsIndividualSynthesizes.Add(new TtsIndividualSynthesize());
                }
            }

            if (_ttsDbContext is not null)
            {
                RequestQueueIngest rqiWithDbRef = await _ttsDbContext.RequestQueueIngest.FindAsync(rqiNoRef.Id);
                rqiWithDbRef.CharacterCostStandard = characterCostStandard;
                rqiWithDbRef.CharacterCostNeural = characterCostNeural;
                await _ttsDbContext.SaveChangesAsync();
            }

            //await _ttsDbContext.SaveChangesAsync();
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

                // This should never happen! But if it does we can find it in the logs.
                if (!rqi.CharacterCostStandard.HasValue || !rqi.CharacterCostNeural.HasValue)
                    _logger.LogWarning("RequestQueueIngest entry {Id} had no cost!", rqi.Id);

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
                    MessageId = rqi.MessageId,
                    CharacterCostStandard = rqi.CharacterCostStandard ?? -1,
                    CharacterCostNeural = rqi.CharacterCostNeural ?? -1
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
