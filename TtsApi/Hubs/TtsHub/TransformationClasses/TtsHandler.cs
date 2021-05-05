using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public class TtsHandler
    {
        public static readonly Dictionary<string, string> ConnectClients = new();
        private readonly ILogger<TtsHandler> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub, ITtsHub> _hubContext;

        public TtsHandler(ILogger<TtsHandler> logger, TtsDbContext ttsDbContext,
            IHubContext<TtsHub, ITtsHub> hubContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _hubContext = hubContext;
        }

        public async Task SendTtsRequest(RequestQueueIngest rqi)
        {
            //SynthesizeSpeechResponse synthResp1 = await Polly.Synthesize("test 1 lllll", VoiceId.Brian, Engine.Standard);
            //SynthesizeSpeechResponse synthResp2 = await Polly.Synthesize("test 2 lllll", VoiceId.Brian, Engine.Standard);

            //TtsRequest ttsRequest = new()
            //{
            //    Id = rqi.MessageId,
            //    MaxMessageTimeSeconds = rqi.Reward.Channel.MaxMessageLength,
            //    TtsIndividualSynthesizes = new List<TtsIndividualSynthesize>
            //    {
            //        new(synthResp1.AudioStream, 1f, 1f),
            //        new(synthResp2.AudioStream, 1f, 1f),
            //    }
            //};

            List<string> clients = ConnectClients
                .Where(pair => pair.Value == rqi.Reward.ChannelId.ToString())
                .Select(pair => pair.Key)
                .Distinct()
                .ToList();
            if (clients.Any())
            {
                //await _hubContext.Clients.Clients(clients).TtsPlayRequest(ttsRequest);
            }
        }

        public void ConfirmTtsSkipped(string contextConnectionId, string? contextUserIdentifier, string id)
        {
        }

        public void ConfirmTtsFullyPlayed(string contextConnectionId, string? contextUserIdentifier, string id)
        {
        }
    }
}
