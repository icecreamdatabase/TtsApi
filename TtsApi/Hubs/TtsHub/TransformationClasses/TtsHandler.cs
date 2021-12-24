using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Aws;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions.DataTypes;
using TtsApi.ExternalApis.Twitch.Helix.Moderation;
using TtsApi.Hubs.TtsHub.TransferClasses;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses
{
    public class TtsHandler
    {
        private static readonly Regex BadWordRegex = new(
            @"(?!nj43)(?:(?:\b(?<!-)|monka)(?:[Nn]|ñ|[Ii7]V)|\/\\\/)[\s\.]*?[liI1y!j\/]+[\s\.]*?(?:[GgbB6934Q🅱qğĜƃ၅5\*][\s\.]*?){2,}(?!arcS|l|Ktlw|ylul|ie217|64)"
        );

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
            _ttsDbContext = serviceProvider.GetService<TtsDbContext>();
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

            /* Global user blacklist */
            if (_ttsDbContext.GlobalUserBlacklist.Any(gub => gub.UserId == rqi.RequesterId))
            {
                await DoneWithPlaying(
                    rqi.Reward.ChannelId,
                    rqi.RedemptionId,
                    MessageType.NotPlayedIsOnGlobalBlacklist
                );
                return;
            }

            /* Channel user blacklist */
            if (rqi.Reward.Channel.ChannelUserBlacklist.Any(cub =>
                    cub.UserId == rqi.RequesterId &&
                    (cub.UntilDate == null || DateTime.Now < cub.UntilDate)
                )
               )
            {
                await DoneWithPlaying(
                    rqi.Reward.ChannelId,
                    rqi.RedemptionId,
                    MessageType.NotPlayedIsOnChannelBlacklist
                );
                return;
            }

            /* Manual filter list */
            if (BadWordRegex.IsMatch(rqi.RawMessage))
            {
                await DoneWithPlaying(
                    rqi.Reward.ChannelId,
                    rqi.RedemptionId,
                    MessageType.BadWordFilter
                );
                return;
            }

            /* Was timed out TODO: or deleted */
            double secondsSinceRequest = (DateTime.Now - rqi.RequestTimestamp).TotalSeconds;
            double waitSRequiredBeforeTimeoutCheck = rqi.Reward.Channel.TimeoutCheckTime - secondsSinceRequest;

            // This shouldn't matter ... but better safe than sorry monkaS
            waitSRequiredBeforeTimeoutCheck = Math.Max(waitSRequiredBeforeTimeoutCheck, -15);
            waitSRequiredBeforeTimeoutCheck = Math.Min(waitSRequiredBeforeTimeoutCheck, 15);

            if (waitSRequiredBeforeTimeoutCheck > 0)
            {
                _logger.LogInformation("Request: {Request} has to wait {Wait} ms",
                    rqi.RedemptionId, (int)(waitSRequiredBeforeTimeoutCheck * 1000));
                await Task.Delay((int)(waitSRequiredBeforeTimeoutCheck * 1000));
            }

            if (rqi.WasTimedOut || ModerationBannedUsers.UserWasTimedOutSinceRedemption(
                    rqi.Reward.ChannelId.ToString(), rqi.RequesterId.ToString(), rqi.RequestTimestamp)
               )
            {
                await DoneWithPlaying(
                    rqi.Reward.ChannelId,
                    rqi.RedemptionId,
                    MessageType.NotPlayedTimedOut
                );
                return;
            }

            /* Sub mode */
            if (rqi.Reward.IsSubOnly && !rqi.IsSubOrHigher)
            {
                await DoneWithPlaying(
                    rqi.Reward.ChannelId,
                    rqi.RedemptionId,
                    MessageType.NotPlayedSubOnly
                );
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

            List<TtsIndividualSynthesize> ttsIndividualSynthesizes = new();

            IEnumerable<TtsMessagePart> messageParts = TtsHandlerStatics.SplitMessage(rqi);
            foreach (TtsMessagePart part in messageParts.Where(part => part != null))
            {
                try
                {
                    SynthesizeSpeechResponse r = await _polly.Synthesize(part.Message, part.VoiceId, part.Engine);
                    TtsIndividualSynthesize tis = new(r.AudioStream, part.PlaybackSpeed, part.Volume);
                    ttsIndividualSynthesizes.Add(tis);

                    if (part.Engine == Engine.Standard)
                        rqi.CharacterCostStandard =
                            rqi.CharacterCostStandard.GetValueOrDefault(0) + r.RequestCharacters;
                    else
                        rqi.CharacterCostNeural =
                            rqi.CharacterCostNeural.GetValueOrDefault(0) + r.RequestCharacters;
                }
                catch (AmazonPollyException e)
                {
                    _logger.LogWarning("GetTtsRequest error: {Message}", e.Message);
                    ttsIndividualSynthesizes.Add(new TtsIndividualSynthesize());
                }
            }

            await _ttsDbContext.SaveChangesAsync();

            ttsRequest.TtsIndividualSynthesizes = ttsIndividualSynthesizes;

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

        private async Task DoneWithPlaying(int roomId, string redemptionId, MessageType reason)
        {
            if (ActiveRequests.TryGetValue(roomId, out string activeRedemptionId) && activeRedemptionId == redemptionId)
            {
                await MoveRqiToTtsLog(redemptionId, reason);
                ActiveRequests.Remove(roomId);
            }
        }

        public async Task MoveRqiToTtsLog(string redemptionId, MessageType reason)
        {
            RequestQueueIngest rqi = await _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .Include(r => r.Reward.Channel)
                .FirstOrDefaultAsync(r => r.RedemptionId == redemptionId);

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
                RedemptionId = rqi.RedemptionId,
                // If skipped before even requesting we won't have a cost
                CharacterCostStandard = rqi.CharacterCostStandard ?? 0,
                CharacterCostNeural = rqi.CharacterCostNeural ?? 0
            });

            TwitchCustomRewardsRedemptionsInput twitchCustomRewardsRedemptionsInput;
            switch (reason)
            {
                case MessageType.SkippedBeforePlaying:
                case MessageType.NotPlayedTimedOut:
                case MessageType.NotPlayedSubOnly:
                case MessageType.NotPlayedIsOnGlobalBlacklist:
                case MessageType.NotPlayedIsOnChannelBlacklist:
                case MessageType.FailedNoParts:
                    twitchCustomRewardsRedemptionsInput = TwitchCustomRewardsRedemptionsInput.Canceled;
                    break;
                case MessageType.PlayedFully:
                case MessageType.Skipped:
                case MessageType.SkippedAfterTime:
                case MessageType.SkippedNoQueue:
                    twitchCustomRewardsRedemptionsInput = TwitchCustomRewardsRedemptionsInput.Fulfilled;
                    break;
                default:
                    twitchCustomRewardsRedemptionsInput = TwitchCustomRewardsRedemptionsInput.Fulfilled;
                    break;
            }

            // always refund special users because why not :^)
            if (_ttsDbContext.BotSpecialUsers.Any(bsu => bsu.UserId == rqi.RequesterId))
                twitchCustomRewardsRedemptionsInput = TwitchCustomRewardsRedemptionsInput.Canceled;

            await _customRewardsRedemptions.UpdateCustomReward(rqi, twitchCustomRewardsRedemptionsInput);

            // Don't remove rqi before we are done with it. (We might have to refresh tokens!!!)
            _ttsDbContext.RequestQueueIngest.Remove(rqi);
            await _ttsDbContext.SaveChangesAsync();
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
