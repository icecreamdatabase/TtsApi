using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace TtsApi.Authentication.Policies.Requirements
{
    public class ChatBotRequirements : IAuthorizationRequirement
    {
        public readonly List<string> Ids = new();

        public ChatBotRequirements()
        {
            Ids.Add("478777352");
        }
    }
}
