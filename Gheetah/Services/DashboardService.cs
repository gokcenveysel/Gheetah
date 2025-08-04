using Gheetah.Interfaces;
using Gheetah.Models.CICDModel;
using Gheetah.Models.ViewModels.Dashboard;
using System.Security.Claims;

namespace Gheetah.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IFileService _fileService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardService(IFileService fileService,IHttpContextAccessor httpContextAccessor)
        {
            _fileService = fileService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DashboardVm> GetDashboardData()
        {
            var userId = GetCurrentUserId();
            var allDashboards = await _fileService.LoadConfigAsync<List<DashboardVm>>("dashboard-settings.json") 
                                ?? new List<DashboardVm>();

            var userDashboard = allDashboards.FirstOrDefault(d => d.UserId == userId) 
                                ?? new DashboardVm { UserId = userId };

            return userDashboard;
        }

        public async Task SaveDashboardWidgets(List<DashboardWidgetVm> widgets)
        {
            var userId = GetCurrentUserId();
            var allDashboards = await _fileService.LoadConfigAsync<List<DashboardVm>>("dashboard-settings.json") 
                                ?? new List<DashboardVm>();

            var existingDashboard = allDashboards.FirstOrDefault(d => d.UserId == userId);
            if (existingDashboard != null)
            {
                existingDashboard.DashboardWidgets = widgets;
            }
            else
            {
                allDashboards.Add(new DashboardVm 
                {
                    UserId = userId,
                    DashboardWidgets = widgets
                });
            }

            await _fileService.SaveConfigAsync("dashboard-settings.json", allDashboards);
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                   ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        public async Task<List<CICDSettingsVm>> GetCICDSettings()
        {
            return await _fileService.LoadConfigAsync<List<CICDSettingsVm>>("cicd-settings.json") 
                   ?? new List<CICDSettingsVm>();
        }
    }
}