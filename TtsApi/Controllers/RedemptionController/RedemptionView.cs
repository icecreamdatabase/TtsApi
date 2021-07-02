using System;
using System.Diagnostics.CodeAnalysis;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RedemptionController
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class RedemptionView
    {
        public int Id { get; }

        public string RewardId { get; }

        public int RequesterId { get; }

        public string RequesterDisplayName { get; }

        public bool IsSubOrHigher { get; }

        public string RawMessage { get; }

        public DateTime RequestTimestamp { get; }

        public RedemptionView(RequestQueueIngest rqi)
        {
            Id = rqi.Id;
            RewardId = rqi.RewardId;
            RequesterId = rqi.RequesterId;
            RequesterDisplayName = rqi.RequesterDisplayName;
            IsSubOrHigher = rqi.IsSubOrHigher;
            RawMessage = rqi.RawMessage;
            RequestTimestamp = rqi.RequestTimestamp;
        }
    }
}
