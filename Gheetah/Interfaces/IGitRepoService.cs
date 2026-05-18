using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;

namespace Gheetah.Interfaces
{
    public interface IGitRepoService
    {
        Task<List<GitRepoVm>> GetReposAsync(RepoSettingsVm setting);
        Task<string> CreateRepositoryAsync(RepoSettingsVm settings, string repoName);
        Task<string> CreatePullRequestAsync(RepoSettingsVm settings, string sourceBranch, string targetBranch, string title, string description);
        bool IsMatch(string repoType);
    }
}