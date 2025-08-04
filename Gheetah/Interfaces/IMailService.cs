using Gheetah.Models.MailSettingsModel;

namespace Gheetah.Interfaces
{
    public interface IMailService
    {
        Task<MailSettingsVm> CreateMailSettings(MailSettingsVm mailSettings);
        Task<MailSettingsVm> UpdateMailSettingsById(string id, MailSettingsVm mailSettings);
        Task<bool> DeleteMailSettingsById(string id);
        Task<List<MailSettingsVm>> GetAllMailSettings();
        Task<MailSettingsVm> GetMailSettingsById(string id);
    }
}
