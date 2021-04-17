using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TtsApi.Authentication.Policies.Requirements;

namespace TtsApi.Authentication.Policies.Handler
{
    public class ChatBotAuthorizationHandler : AuthorizationHandler<ChatBotRequirements>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            ChatBotRequirements requirement)
        {
            bool isInRequiredIDs = requirement.Ids.Any(userId => context.User.HasClaim(AuthClaims.UserId, userId));

            if (isInRequiredIDs)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
