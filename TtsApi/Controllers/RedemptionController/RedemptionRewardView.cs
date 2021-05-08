using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RedemptionController
{
    public class RedemptionRewardView
    {
        public string RewardId { get; }
        public int ChannelId { get; }
        public string VoiceId { get; }
        public bool IsConversation { get; }
        public bool IsSubOnly { get; }
        public int Cooldown { get; }

        public RedemptionRewardView(Reward reward)
        {
            RewardId = reward.RewardId;
            ChannelId = reward.ChannelId;
            VoiceId = reward.VoiceId;
            IsConversation = reward.IsConversation;
            IsSubOnly = reward.IsSubOnly;
            Cooldown = reward.Cooldown;
        }

        public static explicit operator RedemptionRewardView(Reward reward)
        {
            return new(reward);
        }
    }
}
