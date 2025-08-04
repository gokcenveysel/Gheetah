using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;

namespace Gheetah.Interfaces
{
    public interface IGitRepoService
    {
        Task<List<GitRepoVm>> GetReposAsync(RepoSettingsVm setting);
        bool IsMatch(string repoType);
    }
}