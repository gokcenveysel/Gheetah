using Gheetah.Interfaces;
using Gheetah.Models.ViewModels.Setup;
using Microsoft.Extensions.Caching.Memory;

namespace Gheetah.Services
{
    public class DynamicAuthService : IDynamicAuthService
    {
        private readonly IFileService _fileService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DynamicAuthService> _logger;

        private const string GoogleConfigCacheKey = "GoogleConfig";
        private const string AzureConfigCacheKey = "AzureConfig";
        private const string ProviderCacheKey = "AuthProvider";

        public DynamicAuthService(
            IFileService fileService,
            IMemoryCache cache,
            ILogger<DynamicAuthService> logger)
        {
            _fileService = fileService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<AuthProviderType?> GetConfiguredProviderAsync()
        {
            if (_cache.TryGetValue(ProviderCacheKey, out AuthProviderType? cachedProvider))
            {
                return cachedProvider;
            }

            var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            if (File.Exists(Path.Combine(dataPath, "azure-config.json")))
            {
                _cache.Set(ProviderCacheKey, AuthProviderType.Azure, TimeSpan.FromMinutes(30));
                return AuthProviderType.Azure;
            }
            if (File.Exists(Path.Combine(dataPath, "google-config.json")))
            {
                _cache.Set(ProviderCacheKey, AuthProviderType.Google, TimeSpan.FromMinutes(30));
                return AuthProviderType.Google;
            }

            _cache.Set(ProviderCacheKey, AuthProviderType.Custom, TimeSpan.FromMinutes(30));
            return AuthProviderType.Custom;
        }

        public async Task<AzureConfigVm?> GetAzureAsync()
        {
            if (_cache.TryGetValue(AzureConfigCacheKey, out AzureConfigVm? cachedConfig))
            {
                return cachedConfig;
            }

            var config = await _fileService.LoadConfigAsync<AzureConfigVm>("azure-config.json");
            if (config != null && config.IsValid())
            {
                _cache.Set(AzureConfigCacheKey, config, TimeSpan.FromMinutes(30));
                return config;
            }
            _logger.LogWarning("The Azure AD configuration is invalid or not found.");
            return null;
        }

        public async Task<GoogleConfigVm?> GetGoogleAsync()
        {
            if (_cache.TryGetValue(GoogleConfigCacheKey, out GoogleConfigVm? cachedConfig))
            {
                return cachedConfig;
            }

            var config = await _fileService.LoadConfigAsync<GoogleConfigVm>("google-config.json");
            if (config != null && config.IsValid())
            {
                _cache.Set(GoogleConfigCacheKey, config, TimeSpan.FromMinutes(30));
                return config;
            }

            _logger.LogWarning("The Google Workspace configuration is invalid or not found.");
            return null;
        }

        public void ClearCache()
        {
            _cache.Remove(GoogleConfigCacheKey);
            _cache.Remove(AzureConfigCacheKey);
            _cache.Remove(ProviderCacheKey);
        }
    }
}