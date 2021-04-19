using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace TtsApi.Authentication.Policies.Requirements
{
    public class RedemptionsScopesRequirements : IAuthorizationRequirement
    {
        public readonly List<string> RequiredScopes = new() {"channel:manage:redemptions", "channel:read:redemptions"};
    }
}
