using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace TtsApi.Authentication.Policies.Requirements
{
    public class AdminRequirements : IAuthorizationRequirement
    {
        public readonly List<string> Ids = new();

        public AdminRequirements()
        {
            Ids.Add("38949074");
        }
    }
}
