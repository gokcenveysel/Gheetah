using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;

namespace Gheetah.Interfaces
{
    public interface IProjectService
    {
        Task<List<Project>> GetProjectsAsync();
        Task SaveProjectsAsync(List<Project> projects);
        Task AddOrUpdateProjectAsync(Project project);
        Task DeleteProjectAsync(string projectId);
        Task CloneProjectAsync(string repoUrl, RepoSettingsVm repoInfo, string language, string saveDirectory);
        Task UploadLocalProjectAsync(IFormFile archiveFile, string language, string saveDirectory);
        Task<BuildResult> BuildProjectAsync(string projectId, string languageType);
        Task LockProjectAsync(string projectId, string userId);
        Task UnlockProjectAsync(string projectId);
        Task<bool> IsProjectLockedAsync(string projectId);
        Task CheckAndReleaseStaleLocks();
    }
}
