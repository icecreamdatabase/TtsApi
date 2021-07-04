using System;
using System.ComponentModel.DataAnnotations;

namespace TtsApi.Controllers.ChannelBlacklistController
{
    public class ChannelBlacklistInput
    {
        [Required]
        public int UserId { get; set; }

        public DateTime? UntilDate { get; set; }
    }
}
