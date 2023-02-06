using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public class TtsSkipHandler
    {
        private readonly ILogger<TtsSkipHandler> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub, ITtsHub> _ttsHub;
        private readonly TtsRequestHandler _ttsRequestHandler;
        private readonly DoneWithRequest _doneWithRequest;

        public TtsSkipHandler(ILogger<TtsSkipHandler> logger, TtsDbContext ttsDbContext,
            IHubContext<TtsHub, ITtsHub> ttsHub, TtsRequestHandler ttsRequestHandler, DoneWithRequest doneWithRequest)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _ttsHub = ttsHub;
            _ttsRequestHandler = ttsRequestHandler;
            _doneWithRequest = doneWithRequest;
        }

        public void SkipAllRequestsByUserInChannel(string roomId, string userId)
        {
            // TODO: Check the awaiting memes
            SkipAllRequestsByUserInChannel(int.Parse(roomId), int.Parse(userId));
        }

        private async void SkipAllRequestsByUserInChannel(int roomId, int userId)
        {
            List<string> redemptionIds = _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .Where(rqi => rqi.Reward.ChannelId == roomId && rqi.RequesterId == userId)
                .Select(rqi => rqi.RedemptionId)
                .ToList();

            // This needs to happen one at a time!
            foreach (string redemptionId in redemptionIds)
                await SkipTtsRequest(roomId, redemptionId, true);
        }

        public async Task<bool> SkipCurrentTtsRequest(int roomId, bool wasTimedOut = false)
        {
            RequestQueueIngest? rqi = _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .FirstOrDefault(r => r.Reward.ChannelId == roomId);

            if (rqi is null)
                return false;

            return await SkipTtsRequest(roomId, rqi.RedemptionId);
        }

        public async Task<bool> SkipTtsRequest(int roomId, string? redemptoinId = null, bool wasTimedOut = false)
        {
            RequestQueueIngest? rqi = _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .FirstOrDefault(r => r.RedemptionId == redemptoinId);

            if (rqi is null || rqi.Reward.ChannelId != roomId)
                return false;

            // Do we need to skip the currently playing one?
            if (TtsRequestHandler.ActiveRequests.TryGetValue(rqi.Reward.ChannelId, out string? activeRedemptionId) &&
                activeRedemptionId == rqi.RedemptionId)
            {
                List<string> clients = TtsRequestHandler.ConnectClients
                    .Where(pair => pair.Value == roomId.ToString())
                    .Select(pair => pair.Key)
                    .Distinct()
                    .ToList();
                if (clients.Any())
                    await _ttsHub.Clients.Clients(clients).TtsSkipCurrent();
                else
                    await _doneWithRequest.MoveRqiToTtsLog(rqi.RedemptionId, wasTimedOut
                        ? MessageType.NotPlayedTimedOut
                        : MessageType.Skipped
                    );
            }
            else
                await _doneWithRequest.MoveRqiToTtsLog(rqi.RedemptionId, wasTimedOut
                    ? MessageType.NotPlayedTimedOut
                    : MessageType.SkippedBeforePlaying
                );

            return true;
        }
    }
}
