using Gheetah.Interfaces;
using Gheetah.Models.ViewModels.Setup;

namespace Gheetah.Services
{
    public class SetupService : ISetupService
    {
        private readonly IFileService _fileService;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IUserService _userService;
        private readonly IDynamicAuthService _dynamicAuthService;

        public SetupService(
            IFileService fileService,
            IConfiguration config,
            IWebHostEnvironment env,
            IUserService userService,
            IDynamicAuthService dynamicAuthService)
        {
            _fileService = fileService;
            _config = config;
            _env = env;
            _userService = userService;
            _dynamicAuthService = dynamicAuthService;
        }

        public async Task SaveAzureConfigAsync(AzureConfigVm model)
        {
            await _fileService.SaveConfigAsync("azure-config.json", model);
            _dynamicAuthService.ClearCache();
        }

        public async Task SaveGoogleConfigAsync(GoogleConfigVm model)
        {
            await _fileService.SaveConfigAsync("google-config.json", model);
            _dynamicAuthService.ClearCache();
        }

        public async Task<List<GroupVm>> GetGroupsAsync()
        {
            return await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json") ?? new List<GroupVm>();
        }

        public async Task<List<GroupVm>?> GetDefaultGroupsAsync() =>
            _config.GetSection("DefaultGroups").Get<List<GroupVm>>();

        public async Task SaveGroupsAsync(List<GroupVm> groups)
        {
            foreach (var g in groups)
            {
                if (string.IsNullOrWhiteSpace(g.Id))
                    g.Id = g.Name.ToLowerInvariant().Replace(" ", "-") + "-grp";

                if (string.IsNullOrWhiteSpace(g.Description))
                    g.Description = "No description provided";
            }

            await _fileService.SaveConfigAsync("groups.json", groups);
        }

        public async Task<List<PermissionVm>?> GetDefaultPermissionsAsync() =>
            _config.GetSection("DefaultPermissions").Get<List<PermissionVm>>();

        public async Task SaveGroupPermissionsAsync(List<GroupPermissionVm> model)
        {
            var groups = await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json") ?? new();
            var permissions = await GetAllPermissionsAsync();

            foreach (var groupVm in model)
            {
                var group = groups.FirstOrDefault(g => g.Id == groupVm.GroupId);
                if (group != null)
                {
                    group.Permissions = permissions
                        .Where(p => groupVm.SelectedPermissionIds.Contains(p.Id))
                        .ToList();
                }
            }

            await _fileService.SaveConfigAsync("groups.json", groups);
        }

        public async Task<List<PermissionVm>> GetAllPermissionsAsync()
        {
            var permissions = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json");
            if (permissions == null || !permissions.Any())
            {
                var defaultPermissions = await GetDefaultPermissionsAsync();
                if (defaultPermissions.Any())
                {
                    await _fileService.SaveConfigAsync("permissions.json", defaultPermissions);
                    return defaultPermissions;
                }
                return new List<PermissionVm>();
            }
            return permissions;
        }

        public async Task CompleteSetupAsync()
        {
            await _fileService.MarkSetupCompleteAsync();
        }

        public async Task SaveSsoConfigAsync(SsoConfigVm model)
        {
            var ssoConfig = new SsoConfigVm
            {
                AzureConfig = model.AzureConfig,
                GoogleConfig = model.GoogleConfig
            };

            await _fileService.SaveConfigAsync("sso-config.json", ssoConfig);
            _dynamicAuthService.ClearCache();
        }

        public async Task SynchronizeGroupReferencesAsync(List<GroupVm> updatedGroups)
        {
            await _userService.SynchronizeGroupNamesAsync(updatedGroups);

            var permissions = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json") ?? new();
            foreach (var perm in permissions)
            {
                for (int i = 0; i < perm.Actions.Count; i++)
                {
                    var matchedGroup = updatedGroups.FirstOrDefault(g => g.Id == perm.Actions[i] || g.Name == perm.Actions[i]);
                    if (matchedGroup != null)
                    {
                        perm.Actions[i] = matchedGroup.Name;
                    }
                }
            }
            await _fileService.SaveConfigAsync("permissions.json", permissions);
        }
    }
}