using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Org.BouncyCastle.Ocsp;
using TtsApi.Controllers.EventSubController;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class RequestQueueIngest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Reward")]
        public string RewardId { get; set; }

        public virtual Reward Reward { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [Required]
        public string RequesterDisplayName { get; set; }

        [Required]
        public bool IsSubOrHigher { get; set; }

        [Required]
        public string RawMessage { get; set; }

        [Required]
        public string MessageId { get; set; }

        [Required]
        public bool WasTimedOut { get; set; }

        [Required]
        [Column(TypeName = "TIMESTAMP")]
        public DateTime RequestTimestamp { get; set; }

        public int? CharacterCostStandard { get; set; }

        public int? CharacterCostNeural { get; set; }

        public RequestQueueIngest()
        {
        }

        public RequestQueueIngest(EventSubInput<ChannelPointsCustomRewardRedemptionAddCondition,
            ChannelPointsCustomRewardRedemptionEvent> input)
        {
            RewardId = input.Event.Reward.Id;
            RequesterId = int.Parse(input.Event.UserId);
            RequesterDisplayName = input.Event.UserLogin;
            IsSubOrHigher = false; // TODO
            RawMessage = input.Event.UserInput;
            MessageId = input.EventSubHeaders.MessageId;
            WasTimedOut = false; // TODO;
            RequestTimestamp = input.EventSubHeaders.MessageTimestamp;
        }
    }
}
