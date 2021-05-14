using System.Diagnostics.CodeAnalysis;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.DataTypes;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RedemptionController
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class RedemptionRewardView
    {
        public string RewardId { get; }
        public int ChannelId { get; }
        public string VoiceId { get; }
        public bool IsConversation { get; }
        public bool IsSubOnly { get; }
        public int Cooldown { get; }

        public TwitchCustomReward TwitchCustomReward { get; }

        public RedemptionRewardView(Reward reward, TwitchCustomReward twitchCustomReward)
        {
            if (reward is not null)
            {
                RewardId = reward.RewardId;
                ChannelId = reward.ChannelId;
                VoiceId = reward.VoiceId;
                IsConversation = reward.IsConversation;
                IsSubOnly = reward.IsSubOnly;
                Cooldown = reward.Cooldown;
            }

            TwitchCustomReward = twitchCustomReward;
        }
    }
}
