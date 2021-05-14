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
    public class CanChangeSettingsHandler : AuthorizationHandler<CanChangeSettingsRequirements>
    {
        private readonly ILogger<CanChangeSettingsHandler> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly Moderation _moderation;

        public CanChangeSettingsHandler(ILogger<CanChangeSettingsHandler> logger, TtsDbContext ttsDbContext,
            Moderation moderation)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _moderation = moderation;
        }

        protected override async Task<Task> HandleRequirementAsync(AuthorizationHandlerContext context,
            CanChangeSettingsRequirements requirement)
        {
            if (
                context.User.IsInRole(Roles.Roles.IrcBot) ||
                context.User.IsInRole(Roles.Roles.BotOwner) ||
                context.User.IsInRole(Roles.Roles.BotAdmin) ||
                context.User.IsInRole(Roles.Roles.ChannelBroadcaster)
            )
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            string roomIdStr = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(roomIdStr) || !int.TryParse(roomIdStr, out int roomId)) return Task.CompletedTask;
            {
                // Mod check
                // TODO: Mod + ModAreEditors / Editor check
                Channel channel = _ttsDbContext.Channels
                    .Include(c => c.ChannelEditors)
                    .FirstOrDefault(c => c.RoomId == roomId);
                if (channel is not null)
                {
                    string userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == AuthClaims.UserId)?.Value;
                    if (int.TryParse(userIdStr, out int userId))
                        if (channel.ChannelEditors.Any(ce => ce.UserId == userId) ||
                            channel.AllModsAreEditors &&
                            await _moderation.IsModerator(channel, userId)
                        )
                            context.Succeed(requirement);
                }
            }
            return Task.CompletedTask;
        }
    }
}
