﻿using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TtsApi.Authentication.Policies.Requirements;

namespace TtsApi.Authentication.Policies.Handler
{
    public class RedemptionsScopesHandler : AuthorizationHandler<RedemptionsScopesRequirements>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            RedemptionsScopesRequirements requirement)
        {
            bool hasAllRequiredScopes = requirement.RequiredScopes.All(requiredScope =>
            {
                Claim scopesClaim = context.User.Claims.FirstOrDefault(claim => claim.Type == AuthClaims.Scopes);
                return scopesClaim != null &&
                       scopesClaim.Value.ToLowerInvariant().Contains(requiredScope.ToLowerInvariant());
            });

            if (hasAllRequiredScopes)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}