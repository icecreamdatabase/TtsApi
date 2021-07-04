using System.ComponentModel.DataAnnotations;

namespace TtsApi.Controllers.GlobalBlacklistController
{
    public class GlobalBlacklistInput
    {
        [Required]
        public int UserId { get; set; }
    }
}
