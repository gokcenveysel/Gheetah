using Gheetah.Models.ViewModels.Setup;

namespace Gheetah.Interfaces
{
    public interface ISetupService
    {
        Task SaveAzureConfigAsync(AzureConfigVm model);
        Task SaveGoogleConfigAsync(GoogleConfigVm model);
        Task<List<GroupVm>> GetDefaultGroupsAsync();
        Task SaveGroupsAsync(List<GroupVm> groups);
        Task<List<PermissionVm>?> GetDefaultPermissionsAsync();
        Task SaveGroupPermissionsAsync(List<GroupPermissionVm> model);
        Task CompleteSetupAsync();
        Task<List<GroupVm>> GetGroupsAsync();
        Task<List<PermissionVm>> GetAllPermissionsAsync();
        Task SaveSsoConfigAsync(SsoConfigVm model);
        Task SynchronizeGroupReferencesAsync(List<GroupVm> updatedGroups);
    }
}