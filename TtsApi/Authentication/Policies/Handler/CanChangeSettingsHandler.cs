﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TtsApi.Authentication.Policies.Requirements;

namespace TtsApi.Authentication.Policies.Handler
{
    public class CanChangeSettingsHandler : AuthorizationHandler<CanChangeSettingsRequirements>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            CanChangeSettingsRequirements requirement)
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
                // Mod check
                // TODO: Mod + ModAreEditors / Editor check
                if (channelId == 1234)
                    context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}