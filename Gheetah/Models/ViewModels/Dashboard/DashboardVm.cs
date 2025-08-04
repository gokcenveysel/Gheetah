namespace Gheetah.Models.ViewModels.Dashboard
{
    public class DashboardVm
    {
        public string UserId { get; set; }
        public List<DashboardWidgetVm> DashboardWidgets { get; set; } = new List<DashboardWidgetVm>();
    }
}
