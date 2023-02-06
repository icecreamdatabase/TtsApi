using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions.DataTypes;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses;

public class DoneWithRequest
{
    private readonly ILogger<DoneWithRequest> _logger;
    private readonly TtsDbContext _ttsDbContext;
    private readonly CustomRewardsRedemptions _customRewardsRedemptions;

    public DoneWithRequest(ILogger<DoneWithRequest> logger, TtsDbContext ttsDbContext,
        CustomRewardsRedemptions customRewardsRedemptions)
    {
        _logger = logger;
        _ttsDbContext = ttsDbContext;
        _customRewardsRedemptions = customRewardsRedemptions;
    }

    public async Task DoneWithPlaying(int roomId, string redemptionId, MessageType reason)
    {
        if (TtsRequestHandler.ActiveRequests.TryGetValue(roomId, out string? activeRedemptionId) &&
            activeRedemptionId == redemptionId)
        {
            await MoveRqiToTtsLog(redemptionId, reason);
            TtsRequestHandler.ActiveRequests.Remove(roomId);
        }
    }

    public async Task MoveRqiToTtsLog(string redemptionId, MessageType reason)
    {
        RequestQueueIngest? rqi = await _ttsDbContext.RequestQueueIngest
            .Include(r => r.Reward)
            .Include(r => r.Reward.Channel)
            .FirstOrDefaultAsync(r => r.RedemptionId == redemptionId);

        //TODO: this shouldn't happen. This stuff runs in parallel. We need to lock the lockfile Pepega
        if (rqi is null)
            return;

        _ttsDbContext.TtsLogMessages.Add(new TtsLogMessage(rqi, reason));

        TwitchCustomRewardsRedemptionsInput twitchCustomRewardsRedemptionsInput =
            GetRedemptionStatusFromMessageType(reason, rqi);

        await _customRewardsRedemptions.UpdateCustomReward(rqi, twitchCustomRewardsRedemptionsInput);

        // Don't remove rqi before we are done with it. (We might have to refresh tokens!!!)
        _ttsDbContext.RequestQueueIngest.Remove(rqi);
        await _ttsDbContext.SaveChangesAsync();
    }

    private TwitchCustomRewardsRedemptionsInput GetRedemptionStatusFromMessageType(MessageType reason,
        RequestQueueIngest rqi)
    {
        TwitchCustomRewardsRedemptionsInput twitchCustomRewardsRedemptionsInput;
        switch (reason)
        {
            case MessageType.SkippedBeforePlaying:
            case MessageType.NotPlayedTimedOut:
            case MessageType.NotPlayedSubOnly:
            case MessageType.NotPlayedIsOnGlobalBlacklist:
            case MessageType.NotPlayedIsOnChannelBlacklist:
            case MessageType.FailedNoParts:
            case MessageType.BadWordFilter:
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
        return twitchCustomRewardsRedemptionsInput;
    }
}
