using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Gheetah.Authorization
{
    public class PermissionPolicyHandler : AuthorizationHandler<PermissionPolicyRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionPolicyRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Task.CompletedTask;

            var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
            if (roles.Contains(requirement.RequiredRole))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}