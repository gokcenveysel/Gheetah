using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gheetah.ViewComponents
{
    public class GroupDashboardViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var groups = UserClaimsPrincipal.Claims
                .Where(c => c.Type == "group")
                .Select(c => c.Value.Replace(" ", "").Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var group in groups)
            {
                try
                {
                    return View(group); 
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
            }

            return View("Default");
        }
    }
}