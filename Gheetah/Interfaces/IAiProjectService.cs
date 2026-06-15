using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IAiProjectService
    {
        Task<List<AiProject>> GetProjectsAsync();
        Task<AiProject> GetByIdAsync(string id);
        Task SaveProjectAsync(AiProject project);
        Task DeleteProjectAsync(string id);
    }
}
