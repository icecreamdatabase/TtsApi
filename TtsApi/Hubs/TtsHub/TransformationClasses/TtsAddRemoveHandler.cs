using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Controllers.EventSubController;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public class TtsAddRemoveHandler
    {
        private readonly ILogger<TtsAddRemoveHandler> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub, ITtsHub> _ttsHub;
        private readonly TtsHandler _ttsHandler;

        public TtsAddRemoveHandler(ILogger<TtsAddRemoveHandler> logger, TtsDbContext ttsDbContext,
            IHubContext<TtsHub, ITtsHub> ttsHub, TtsHandler ttsHandler)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _ttsHub = ttsHub;
            _ttsHandler = ttsHandler;
        }

        public void CreateNewTtsRequest(EventSubInput<ChannelPointsCustomRewardRedemptionAddCondition,
            ChannelPointsCustomRewardRedemptionEvent> input)
        {
            if (!_ttsDbContext.Rewards.Any(reward => reward.RewardId == input.Event.Reward.Id))
                return;

            _ttsDbContext.RequestQueueIngest.Add(new RequestQueueIngest(input));
            _ttsDbContext.SaveChanges();
        }

        public void SkipAllRequestsByUserInChannel(string roomId, string userId)
        {
            // TODO: Check the awaiting memes
            SkipAllRequestsByUserInChannel(int.Parse(roomId), int.Parse(userId));
        }

        private async void SkipAllRequestsByUserInChannel(int roomId, int userId)
        {
            List<string> messageIds = _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .Where(rqi => rqi.Reward.ChannelId == roomId && rqi.RequesterId == userId)
                .Select(rqi => rqi.MessageId)
                .ToList();

            // This needs to happen one at a time!
            foreach (string messageId in messageIds)
                await SkipTtsRequest(roomId, messageId, true);
        }

        public async Task<bool> SkipCurrentTtsRequest(int roomId, bool wasTimedOut = false)
        {
            RequestQueueIngest rqi = _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .FirstOrDefault(r => r.Reward.ChannelId == roomId);

            if (rqi is null)
                return false;

            return await SkipTtsRequest(roomId, rqi.MessageId);
        }

        public async Task<bool> SkipTtsRequest(int roomId, string messageId = null, bool wasTimedOut = false)
        {
            RequestQueueIngest rqi = _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .FirstOrDefault(r => r.MessageId == messageId);

            if (rqi is null || rqi.Reward.ChannelId != roomId)
                return false;

            // Do we need to skip the currently playing one?
            if (TtsHandler.ActiveRequests.TryGetValue(rqi.Reward.ChannelId, out string activeMessageId) &&
                activeMessageId == rqi.MessageId)
            {
                List<string> clients = TtsHandler.ConnectClients
                    .Where(pair => pair.Value == roomId.ToString())
                    .Select(pair => pair.Key)
                    .Distinct()
                    .ToList();
                if (clients.Any())
                    await _ttsHub.Clients.Clients(clients).TtsSkipCurrent();
                else
                    await _ttsHandler.MoveRqiToTtsLog(rqi.MessageId, wasTimedOut
                        ? MessageType.NotPlayedTimedOut
                        : MessageType.Skipped
                    );
            }
            else
                await _ttsHandler.MoveRqiToTtsLog(rqi.MessageId, wasTimedOut
                    ? MessageType.NotPlayedTimedOut
                    : MessageType.SkippedBeforePlaying
                );

            return true;
        }
    }
}
