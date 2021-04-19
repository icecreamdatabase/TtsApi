using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TtsApi.Authentication.Policies.Requirements;

namespace TtsApi.Authentication.Policies.Handler
{
    public class ChannelModHandler : AuthorizationHandler<ChannelModRequirements>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            ChannelModRequirements requirement)
        {
            RouteValueDictionary routeValues = (context.Resource as DefaultHttpContext)?.Request.RouteValues;

            if (routeValues == null)
                return Task.CompletedTask;

            // Get channelId from Route
            routeValues.TryGetValue("channelId", out object channelIdStr);
            if (channelIdStr != null && int.TryParse(channelIdStr.ToString(), out int channelId))
            {
                // Mod check
                // TODO: Mod check
                if (channelId == 1234)
                    context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
