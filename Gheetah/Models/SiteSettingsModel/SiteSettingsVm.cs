using Gheetah.Models.CICDModel;
using Gheetah.Models.MailSettingsModel;
using Gheetah.Models.RepoSettingsModel;

namespace Gheetah.Models.SiteSettingsModel
{
    public class SiteSettingsVm
    {
        public List<RepoSettingsVm> RepoSettingsList { get; set; }
        public List<CICDSettingsVm> CICDSettingsList { get; set; }
        public List<MailSettingsVm> MailSettingsList { get; set; }
    }
}
