using Microsoft.AspNetCore.Authorization;

namespace Gheetah.Authorization
{
    public class PermissionPolicyRequirement : IAuthorizationRequirement
    {
        public string RequiredRole { get; }

        public PermissionPolicyRequirement(string requiredRole)
        {
            RequiredRole = requiredRole;
        }
    }
}