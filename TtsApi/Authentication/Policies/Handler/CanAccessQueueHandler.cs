using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TtsApi.Authentication.Policies.Requirements;

namespace TtsApi.Authentication.Policies.Handler
{
    public class CanAccessQueueHandler : AuthorizationHandler<CanAccessQueueRequirements>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            CanAccessQueueRequirements requirement)
        {
            if (
                context.User.IsInRole(Roles.Roles.ChannelBroadcaster) ||
                context.User.IsInRole(Roles.Roles.Admin)
            )
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            RouteValueDictionary routeValues = (context.Resource as DefaultHttpContext)?.Request.RouteValues;

            if (routeValues == null)
                return Task.CompletedTask;

            // Get channelId from Route
            routeValues.TryGetValue("channelId", out object channelIdStr);
            if (channelIdStr != null && int.TryParse(channelIdStr.ToString(), out int channelId))
            {
                // Get userId from OAuth claim
                Claim userIdClaim = context.User.Claims.FirstOrDefault(claim => claim.Type == AuthClaims.UserId);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Mod check
                    // TODO: Mod / Editor check
                    if (channelId == 1234)
                        context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
