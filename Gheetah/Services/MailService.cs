using Gheetah.Interfaces;
using Gheetah.Models.MailSettingsModel;

namespace Gheetah.Services
{
    public class MailService : IMailService
    {
        private readonly IFileService _fileService;
        private readonly string _fileName = "mail-settings.json";

        public MailService(IFileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        public async Task<MailSettingsVm> CreateMailSettings(MailSettingsVm mailSettings)
        {
            if (mailSettings == null)
                throw new ArgumentNullException(nameof(mailSettings));

            // Generate a new ID if not provided
            if (string.IsNullOrEmpty(mailSettings.Id))
            {
                mailSettings.Id = Guid.NewGuid().ToString();
            }

            var allSettings = await GetAllMailSettings();
            allSettings.Add(mailSettings);
            
            await _fileService.SaveConfigAsync(_fileName, allSettings);
            return mailSettings;
        }

        public async Task<MailSettingsVm> UpdateMailSettingsById(string id, MailSettingsVm mailSettings)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Id cannot be null or empty", nameof(id));

            if (mailSettings == null)
                throw new ArgumentNullException(nameof(mailSettings));

            var allSettings = await GetAllMailSettings();
            var existingSetting = allSettings.FirstOrDefault(s => s.Id == id);

            if (existingSetting == null)
                throw new KeyNotFoundException($"Mail settings with id {id} not found");

            // Update properties
            existingSetting.Provider = mailSettings.Provider;
            existingSetting.Name = mailSettings.Name;
            existingSetting.IsActive = mailSettings.IsActive;
            existingSetting.LastTestDate = mailSettings.LastTestDate;

            // Update provider-specific properties
            existingSetting.SmtpHost = mailSettings.SmtpHost;
            existingSetting.SmtpPort = mailSettings.SmtpPort;
            existingSetting.SmtpUsername = mailSettings.SmtpUsername;
            existingSetting.SmtpPassword = mailSettings.SmtpPassword;
            existingSetting.SmtpUseSsl = mailSettings.SmtpUseSsl;
            existingSetting.SendGridApiKey = mailSettings.SendGridApiKey;
            existingSetting.AzureTenantId = mailSettings.AzureTenantId;
            existingSetting.AzureClientId = mailSettings.AzureClientId;
            existingSetting.AzureClientSecret = mailSettings.AzureClientSecret;
            existingSetting.AzureEndpoint = mailSettings.AzureEndpoint;

            await _fileService.SaveConfigAsync(_fileName, allSettings);
            return existingSetting;
        }

        public async Task<bool> DeleteMailSettingsById(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Id cannot be null or empty", nameof(id));

            var allSettings = await GetAllMailSettings();
            var settingToRemove = allSettings.FirstOrDefault(s => s.Id == id);

            if (settingToRemove == null)
                return false;

            allSettings.Remove(settingToRemove);
            await _fileService.SaveConfigAsync(_fileName, allSettings);
            return true;
        }

        public async Task<List<MailSettingsVm>> GetAllMailSettings()
        {
            var settings = await _fileService.LoadConfigAsync<List<MailSettingsVm>>(_fileName);
            return settings ?? new List<MailSettingsVm>();
        }

        public async Task<MailSettingsVm> GetMailSettingsById(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Id cannot be null or empty", nameof(id));

            var allSettings = await GetAllMailSettings();
            return allSettings.FirstOrDefault(s => s.Id == id);
        }
    }
}
