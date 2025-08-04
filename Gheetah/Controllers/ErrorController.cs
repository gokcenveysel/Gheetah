using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    public class ErrorController : Controller
    {
        [Route("/Error")]
        public IActionResult HandleError() => View();
    }
}
