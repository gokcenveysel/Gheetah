using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IEnvironmentService
    {
        Task<List<EnvironmentConfig>> GetEnvironmentsAsync(string projectId = null);
        Task<EnvironmentConfig> GetByIdAsync(string id);
        Task SaveEnvironmentAsync(EnvironmentConfig env);
        Task DeleteEnvironmentAsync(string id);
        Task<EnvironmentConfig> GetDefaultForProjectAsync(string projectId);
        Task<Dictionary<string, string>> ResolveVariablesAsync(string envId);
    }
}
