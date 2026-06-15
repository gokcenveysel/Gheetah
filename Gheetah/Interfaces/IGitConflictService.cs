using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IGitConflictService
    {
        Task<bool> HasConflictsAsync(string repoPath);
        Task<List<ConflictBlock>> ParseConflictsAsync(string filePath);
        Task<List<string>> GetConflictedFilesAsync(string repoPath);
        Task ApplyResolutionsAsync(string repoPath, string filePath, List<ResolvedBlock> resolutions);
        Task CommitResolutionAsync(string repoPath, string commitMessage, string authorName, string authorEmail);
    }
}
