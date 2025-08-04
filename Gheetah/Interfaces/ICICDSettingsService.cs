using Gheetah.Models.CICDModel;

namespace Gheetah.Interfaces
{
    public interface ICICDSettingsService
    {
        Task<List<CICDSettingsVm>> GetAllAsync();
        Task<CICDSettingsVm?> GetByIdAsync(string id);
        Task SaveAsync(CICDSettingsVm config);
        Task UpdateAsync(CICDSettingsVm config);
        Task DeleteAsync(string id);
    }
}
