using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TtsApi.ExternalApis.Twitch.Helix.Moderation;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs.TtsHub.TransformationClasses;

public partial class TtsHandler
{
    private async Task<bool> CheckGlobalUserBlacklist(RequestQueueIngest rqi)
    {
        /* Global user blacklist */
        if (_ttsDbContext.GlobalUserBlacklist.Any(gub => gub.UserId == rqi.RequesterId))
        {
            await DoneWithPlaying(
                rqi.Reward.ChannelId,
                rqi.RedemptionId,
                MessageType.NotPlayedIsOnGlobalBlacklist
            );
            return true;
        }

        return false;
    }

    private async Task<bool> CheckChannelUserBlacklist(RequestQueueIngest rqi)
    {
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
            return true;
        }

        return false;
    }

    private static readonly Regex BadWordRegex = new(
        @"(?!nj43)(?:(?:\b(?<!-)|monka)(?:[Nn]|ñ|[Ii7]V)|\/\\\/)[\s\.]*?[liI1y!j\/]+[\s\.]*?(?:[GgbB6934Q🅱qğĜƃ၅5\*][\s\.]*?){2,}(?!arcS|l|Ktlw|ylul|ie217|64)"
    );

    private async Task<bool> CheckManualFilterList(RequestQueueIngest rqi)
    {
        /* Manual filter list */
        if (BadWordRegex.IsMatch(rqi.RawMessage))
        {
            await DoneWithPlaying(
                rqi.Reward.ChannelId,
                rqi.RedemptionId,
                MessageType.BadWordFilter
            );
            return true;
        }

        return false;
    }

    private async Task<bool> CheckWasTimedOut(RequestQueueIngest rqi)
    {
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
            return true;
        }

        return false;
    }

    private async Task<bool> CheckSubMode(RequestQueueIngest rqi)
    {
        /* Sub mode */
        if (rqi.Reward.IsSubOnly && !rqi.IsSubOrHigher)
        {
            await DoneWithPlaying(
                rqi.Reward.ChannelId,
                rqi.RedemptionId,
                MessageType.NotPlayedSubOnly
            );
            return true;
        }

        return false;
    }
}
