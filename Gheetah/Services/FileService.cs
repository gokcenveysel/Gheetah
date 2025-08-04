using Gheetah.Interfaces;
using Gheetah.Models.ViewModels.Setup;
using System.Text.Json;

namespace Gheetah.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileService> _logger;
        private readonly string _dataPath;

        public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataPath = Path.Combine(_env.ContentRootPath, "Data");
            EnsureDataDirectoryExists();
        }

        private void EnsureDataDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_dataPath))
                {
                    Directory.CreateDirectory(_dataPath);
                    _logger.LogInformation("Data directory created at: {Path}", _dataPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to create data directory");
                throw;
            }
        }

        public async Task SaveAzureConfigAsync(AzureConfigVm config)
        {
            await SaveConfigAsync("azure-config.json", config);
        }

        public async Task<bool> AzureConfigExistsAsync()
        {
            return await ConfigExistsAsync("azure-config.json");
        }

        public async Task SaveGoogleConfigAsync(GoogleConfigVm config)
        {
            await SaveConfigAsync("google-config.json", config);
        }

        public async Task<bool> GoogleConfigExistsAsync()
        {
            return await ConfigExistsAsync("google-config.json");
        }

        public async Task MarkSetupCompleteAsync()
        {
            var setupFile = Path.Combine(_dataPath, "setup_completed.json");
            try
            {
                await File.WriteAllTextAsync(setupFile, JsonSerializer.Serialize(new { Completed = true }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark setup complete: {FilePath}", setupFile);
                throw;
            }
        }

        public bool IsSetupComplete()
        {
            var setupFile = Path.Combine(_dataPath, "setup_completed.json");
            try
            {
                bool isComplete = File.Exists(setupFile);
                return isComplete;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking IsSetupComplete: {FilePath}", setupFile);
                return false;
            }
        }

        public async Task<bool> ConfigExistsAsync(string fileName)
        {
            var filePath = Path.Combine(_dataPath, fileName);
            bool exists = File.Exists(filePath);
            return exists;
        }
        
        public async Task<T> LoadConfigAsync<T>(string fileName)
        {
            var filePath = Path.Combine(_dataPath, fileName);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Config file not found: {FilePath}", filePath);
                return default;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogWarning("Config file is empty: {FilePath}", filePath);
                    return default;
                }

                if (typeof(T) == typeof(string))
                {
                    if (json.StartsWith("\"") && json.EndsWith("\""))
                    {
                        return (T)(object)JsonSerializer.Deserialize<string>(json);
                    }
                    throw new JsonException($"Expected a JSON string in {fileName}, but found invalid format: {json}");
                }

                var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize {FileName}", fileName);
                throw new InvalidOperationException($"Failed to deserialize {fileName}: {ex.Message}", ex);
            }
        }

        public async Task SaveConfigAsync<T>(string fileName, T data)
        {
            var filePath = Path.Combine(_dataPath, fileName);
            try
            {
                if (data is string stringContent)
                {
                    var json = JsonSerializer.Serialize(stringContent, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, json);
                }
                else
                {
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save {FileName}", fileName);
                throw new InvalidOperationException($"Failed to save {fileName}: {ex.Message}", ex);
            }
        }

        public async Task<string[]> ReadAllLinesAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                throw new FileNotFoundException($"File not found at '{filePath}'", filePath);
            }
            return await File.ReadAllLinesAsync(filePath);
        }

        public async Task WriteAllLinesAsync(string filePath, IEnumerable<string> lines)
        {
            await File.WriteAllLinesAsync(filePath, lines);
        }

        public async Task<string> ReadAllTextAsync(string filePath, int retryCount = 3)
        {
            while (true)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("File not found: {FilePath}", filePath);
                        throw new FileNotFoundException($"File not found at '{filePath}'", filePath);
                    }
                    var content = await File.ReadAllTextAsync(filePath);
                    return content;
                }
                catch (IOException) when (retryCount-- > 0)
                {
                    _logger.LogWarning("IOException reading file, retrying... ({RetriesLeft} left)", retryCount);
                    await Task.Delay(100);
                }
            }
        }

        public async Task WriteAllTextAsync(string filePath, string content, int retryCount = 3)
        {
            while (true)
            {
                try
                {
                    await File.WriteAllTextAsync(filePath, content);
                    return;
                }
                catch (IOException) when (retryCount-- > 0)
                {
                    _logger.LogWarning("IOException writing file, retrying... ({RetriesLeft} left)", retryCount);
                    await Task.Delay(100);
                }
            }
        }

        public async Task DeleteAsync(string filePath, int retryCount = 3)
        {
            while (retryCount > 0)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    return;
                }
                catch (IOException ex) when (retryCount-- > 0)
                {
                    _logger.LogWarning(ex, "File delete failed, retrying... ({RetriesLeft} left)", retryCount);
                    await Task.Delay(100);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "Permission denied for file: {FilePath}", filePath);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error deleting file: {FilePath}", filePath);
                    throw;
                }
            }
            throw new IOException($"Failed to delete file after multiple attempts: {filePath}");
        }

        public async Task DeleteDirectoryAsync(string directoryPath, int retryCount = 3)
        {
            while (retryCount > 0)
            {
                try
                {
                    if (Directory.Exists(directoryPath))
                    {
                        Directory.Delete(directoryPath, true);
                    }
                    return;
                }
                catch (IOException ex) when (retryCount-- > 0)
                {
                    _logger.LogWarning(ex, "Directory delete failed, retrying... ({RetriesLeft} left)", retryCount);
                    await Task.Delay(300);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "Permission denied for directory: {DirectoryPath}", directoryPath);
                    throw;
                }
            }
            throw new IOException($"Failed to delete directory after multiple attempts: {directoryPath}");
        }

        public async Task MoveAsync(string sourceFilePath, string destFilePath, bool overwrite = false, int retryCount = 3)
        {
            while (retryCount > 0)
            {
                try
                {
                    if (!File.Exists(sourceFilePath))
                    {
                        throw new FileNotFoundException($"Source file not found: {sourceFilePath}", sourceFilePath);
                    }

                    var destDir = Path.GetDirectoryName(destFilePath);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    if (File.Exists(destFilePath) && overwrite)
                    {
                        File.Delete(destFilePath);
                    }

                    File.Move(sourceFilePath, destFilePath);
                    return;
                }
                catch (IOException ex) when (retryCount-- > 0)
                {
                    _logger.LogWarning(ex, "File move failed, retrying... ({RetriesLeft} left)", retryCount);
                    await Task.Delay(300);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError(ex, "Permission denied for file operation: {Source} -> {Destination}", sourceFilePath, destFilePath);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error moving file: {Source} -> {Destination}", sourceFilePath, destFilePath);
                    throw;
                }
            }
            
            throw new IOException($"Failed to move file after multiple attempts: {sourceFilePath}");
        }
        
        public async Task<AuthProviderType> LoadAuthProviderTypeAsync()
        {
            var authTypeFile = Path.Combine(_dataPath, "auth-provider-type.json");
            if (!File.Exists(authTypeFile))
            {
                _logger.LogWarning("Auth provider type file not found: {FilePath}", authTypeFile);
                return AuthProviderType.None;
            }
            
            var json = await File.ReadAllTextAsync(authTypeFile);
            var provider = JsonSerializer.Deserialize<AuthProviderType>(json);
            return Enum.IsDefined(typeof(AuthProviderType), provider) ? provider : AuthProviderType.None;
        }

        public async Task SyncGroupNamesInPermissions(List<GroupVm> updatedGroups)
        {
            var permissions = await LoadConfigAsync<List<PermissionVm>>("permissions.json");
            if (permissions == null || permissions.Count == 0)
            {
                _logger.LogWarning("No permissions found to sync: permissions.json");
                return;
            }

            var groupIdToNameMap = updatedGroups.ToDictionary(g => g.Id, g => g.Name);

            foreach (var perm in permissions)
            {
                if (groupIdToNameMap.TryGetValue(perm.Id, out var newName))
                {
                    perm.Name = newName;
                }
            }

            await SaveConfigAsync("permissions.json", permissions);
            _logger.LogInformation("Group names synced in permissions.json");
        }
    }
}