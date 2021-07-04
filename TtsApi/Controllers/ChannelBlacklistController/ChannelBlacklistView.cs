using System;
using System.Diagnostics.CodeAnalysis;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.ChannelBlacklistController
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class ChannelBlacklistView
    {
        public int RoomId { get; }
        public int UserId { get; }
        public DateTime AddDate { get; }
        public DateTime? UntilDate { get; }

        public ChannelBlacklistView(ChannelUserBlacklist cub)
        {
            RoomId = cub.ChannelId;
            UserId = cub.UserId;
            AddDate = cub.AddDate;
            UntilDate = cub.UntilDate;
        }
    }
}
