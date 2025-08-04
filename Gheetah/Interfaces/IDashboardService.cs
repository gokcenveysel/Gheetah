using Gheetah.Models.CICDModel;
using Gheetah.Models.ViewModels.Dashboard;

namespace Gheetah.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardVm> GetDashboardData();
        Task SaveDashboardWidgets(List<DashboardWidgetVm> widgets);
        Task<List<CICDSettingsVm>> GetCICDSettings();
    }
}
