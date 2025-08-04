using Gheetah.Models.ViewModels.Setup;

namespace Gheetah.Interfaces
{
    public interface IFileService
    {
        Task SaveAzureConfigAsync(AzureConfigVm config);

        Task<bool> AzureConfigExistsAsync();

        Task SaveGoogleConfigAsync(GoogleConfigVm config);

        Task<bool> GoogleConfigExistsAsync();

        Task MarkSetupCompleteAsync();

        bool IsSetupComplete();
        
        Task<bool> ConfigExistsAsync(string fileName);

        Task<T> LoadConfigAsync<T>(string fileName);

        Task SaveConfigAsync<T>(string fileName, T data);
        
        Task<AuthProviderType> LoadAuthProviderTypeAsync();

        Task SyncGroupNamesInPermissions(List<GroupVm> updatedGroups);

        Task WriteAllTextAsync(string filePath, string content, int retryCount = 3);

        Task<string> ReadAllTextAsync(string filePath, int retryCount = 3);

        Task WriteAllLinesAsync(string filePath, IEnumerable<string> lines);

        Task<string[]> ReadAllLinesAsync(string filePath);

        Task DeleteAsync(string filePath, int retryCount = 3);

        Task DeleteDirectoryAsync(string directoryPath, int retryCount = 3);

        Task MoveAsync(string sourceFilePath, string destFilePath, bool overwrite = false, int retryCount = 3);
    }
}
