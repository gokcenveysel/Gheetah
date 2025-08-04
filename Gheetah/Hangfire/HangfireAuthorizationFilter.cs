using Hangfire.Dashboard;

namespace Gheetah.Hangfire
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Lead");
        }
    }
}
