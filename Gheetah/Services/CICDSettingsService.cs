using Gheetah.Interfaces;
using Gheetah.Models.CICDModel;

namespace Gheetah.Services
{
    public class CICDSettingsService : ICICDSettingsService
    {
        private readonly IFileService _fileService;
        private readonly string _filePath;

        public CICDSettingsService(IFileService fileService)
        {
            _fileService = fileService;
            _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "cicd-settings.json");
        }

        public async Task<List<CICDSettingsVm>> GetAllAsync()
        {
            var settings = await _fileService.LoadConfigAsync<List<CICDSettingsVm>>(_filePath);
            return settings ?? new List<CICDSettingsVm>();
        }

        public async Task<CICDSettingsVm?> GetByIdAsync(string id)
        {
            var settings = await GetAllAsync();
            return settings.FirstOrDefault(s => s.Id == id);
        }

        public async Task SaveAsync(CICDSettingsVm config)
        {
            var settings = await GetAllAsync();
            settings.Add(config);
            await _fileService.SaveConfigAsync(_filePath, settings);
        }

        public async Task UpdateAsync(CICDSettingsVm config)
        {
            var settings = await GetAllAsync();
            var existingConfig = settings.FirstOrDefault(s => s.Id == config.Id);

            if (existingConfig != null)
            {
                settings.Remove(existingConfig);
                settings.Add(config);
                await _fileService.SaveConfigAsync(_filePath, settings);
            }
        }

        public async Task DeleteAsync(string id)
        {
            var settings = await GetAllAsync();
            var configToDelete = settings.FirstOrDefault(s => s.Id == id);

            if (configToDelete != null)
            {
                settings.Remove(configToDelete);
                await _fileService.SaveConfigAsync(_filePath, settings);
            }
        }
    }
}
