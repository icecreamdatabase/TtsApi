using System;
using System.Diagnostics.CodeAnalysis;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.GlobalBlacklistController
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class globalBlacklistView
    {
        public int UserId { get; }
        public DateTime AddDate { get; }

        public globalBlacklistView(GlobalUserBlacklist gub)
        {
            UserId = gub.UserId;
            AddDate = gub.AddDate;
        }
    }
}
