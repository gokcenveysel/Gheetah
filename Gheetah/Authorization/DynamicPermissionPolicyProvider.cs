using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Gheetah.Models.ViewModels.Setup;
using Gheetah.Interfaces;

namespace Gheetah.Authorization
{
    public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly IAuthorizationPolicyProvider _fallbackPolicyProvider;
        private readonly IFileService _fileService;

        public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options, IFileService fileService)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
            _fileService = fileService;
        }

        public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith("Dynamic_", StringComparison.OrdinalIgnoreCase))
            {
                var idsPart = policyName.Replace("Dynamic_", "");
                var ids = idsPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var permissions = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json");
                if (permissions == null)
                    return null;

                var matchedRoles = permissions
                    .Where(p => ids.Contains(p.Id))
                    .Select(p => p.Name)
                    .ToList();

                if (!matchedRoles.Any())
                    return null;

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAssertion(context =>
                    {
                        var userRoles = context.User.Claims
                            .Where(c => c.Type == ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToList();

                        return userRoles.Any(r => matchedRoles.Contains(r));
                    })
                    .Build();

                return policy;
            }

            return await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }
}