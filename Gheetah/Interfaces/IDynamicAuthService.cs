using Gheetah.Models.ViewModels.Setup;

namespace Gheetah.Interfaces
{
    public interface IDynamicAuthService
    {
        Task<AuthProviderType?> GetConfiguredProviderAsync();
        Task<AzureConfigVm?> GetAzureAsync();
        Task<GoogleConfigVm?> GetGoogleAsync();
        void ClearCache();
    }
}
