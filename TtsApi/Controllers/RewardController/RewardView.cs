using System.Diagnostics.CodeAnalysis;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.CustomRewards.DataTypes;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RewardController
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class RewardView
    {
        public string RewardId { get; }
        public int ChannelId { get; }
        public string VoiceId { get; }
        public string VoiceEngine { get; }
        public bool IsConversation { get; }
        public bool IsSubOnly { get; }
        public int Cooldown { get; }

        public TwitchCustomRewards TwitchCustomRewards { get; }

        public RewardView(Reward reward, TwitchCustomRewards twitchCustomRewards)
        {
            if (reward is not null)
            {
                RewardId = reward.RewardId;
                ChannelId = reward.ChannelId;
                VoiceId = reward.VoiceId;
                VoiceEngine = reward.VoiceEngine;
                IsConversation = reward.IsConversation;
                IsSubOnly = reward.IsSubOnly;
                Cooldown = reward.Cooldown;
            }

            TwitchCustomRewards = twitchCustomRewards;
        }
    }
}
