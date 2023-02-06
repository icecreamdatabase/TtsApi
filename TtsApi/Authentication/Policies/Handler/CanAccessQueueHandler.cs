using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies.Requirements;
using TtsApi.ExternalApis.Twitch.Helix.Moderation;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Authentication.Policies.Handler
{
    public class CanAccessQueueHandler : AuthorizationHandler<CanAccessQueueRequirements>
    {
        private readonly ILogger<CanAccessQueueHandler> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly Moderation _moderation;

        public CanAccessQueueHandler(ILogger<CanAccessQueueHandler> logger, TtsDbContext ttsDbContext,
            Moderation moderation)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _moderation = moderation;
        }


        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
            CanAccessQueueRequirements requirement)
        {
            if (
                context.User.IsInRole(Roles.Roles.IrcBot) ||
                context.User.IsInRole(Roles.Roles.BotOwner) ||
                context.User.IsInRole(Roles.Roles.BotAdmin) ||
                context.User.IsInRole(Roles.Roles.ChannelBroadcaster)
            )
            {
                context.Succeed(requirement);
                return;
            }

            string roomIdStr = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            string userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == AuthClaims.UserId)?.Value;
            if (
                string.IsNullOrEmpty(roomIdStr) || !int.TryParse(roomIdStr, out int roomId) ||
                string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)
            )
                return;

            Channel channel = _ttsDbContext.Channels
                .Include(c => c.ChannelEditors)
                .FirstOrDefault(c => c.RoomId == roomId);
            if (channel is null)
                return;

            // Is Editor or is Mod 
            if (
                channel.ChannelEditors.Any(ce => ce.UserId == userId) ||
                await _moderation.IsModerator(channel, userId)
            )
                context.Succeed(requirement);
        }
    }
}
