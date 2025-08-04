using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    public class GroupController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
