using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Polly.Model;
using Microsoft.AspNetCore.SignalR;
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

        public TtsHandler(ILogger<TtsHandler> logger, TtsDbContext ttsDbContext,
            IHubContext<TtsHub, ITtsHub> hubContext, Polly polly)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _hubContext = hubContext;
            _polly = polly;
        }

        public async Task SendTtsRequest(RequestQueueIngest rqi)
        {
            List<string> clients = ConnectClients
                .Where(pair => pair.Value == rqi.Reward.ChannelId.ToString())
                .Select(pair => pair.Key)
                .Distinct()
                .ToList();
            if (clients.Any())
            {
                if (ActiveRequests.ContainsKey(rqi.Reward.ChannelId))
                    return;

                ActiveRequests.Add(rqi.Reward.ChannelId, rqi.Id.ToString());

                TtsRequest ttsRequest = await GetTtsRequest(rqi);

                if (ttsRequest.TtsIndividualSynthesizes.Count > 0)
                    await _hubContext.Clients.Clients(clients).TtsPlayRequest(ttsRequest);
                else
                    throw new Exception("No message parts?");
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
                SynthesizeSpeechResponse r = await _polly.Synthesize(part.Message, part.VoiceId, part.Engine);
                TtsIndividualSynthesize tis = new(r.AudioStream, part.PlaybackSpeed, part.Volume);
                ttsIndividualSynthesizes.Add(tis);
            }

            ttsRequest.TtsIndividualSynthesizes = ttsIndividualSynthesizes;

            return ttsRequest;
        }

        public async Task ConfirmTtsSkipped(string contextConnectionId, string contextUserIdentifier, string id)
        {
            await DoneWithPlaying(contextUserIdentifier, id, "skipped");
        }

        public async Task ConfirmTtsFullyPlayed(string contextConnectionId, string contextUserIdentifier, string id)
        {
            await DoneWithPlaying(contextUserIdentifier, id, "Played");
        }

        private async Task DoneWithPlaying(string contextUserIdentifier, string id, string reason)
        {
            int roomId = int.Parse(contextUserIdentifier);
            if (ActiveRequests[roomId] == id)
            {
                ActiveRequests.Remove(roomId);
                RequestQueueIngest rqi = await _ttsDbContext.RequestQueueIngest.FindAsync(int.Parse(id));
                //TODO: add rqi to messageLog with note from reason parameter
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
