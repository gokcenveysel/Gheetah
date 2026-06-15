using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    [Route("Environment")]
    [Authorize]
    public class EnvironmentController : Controller
    {
        private readonly IEnvironmentService _environmentService;
        private readonly ILogger<EnvironmentController> _logger;

        public EnvironmentController(IEnvironmentService environmentService, ILogger<EnvironmentController> logger)
        {
            _environmentService = environmentService;
            _logger = logger;
        }

        [HttpGet("GetForProject/{projectId}")]
        public async Task<IActionResult> GetForProject(string projectId)
        {
            var environments = await _environmentService.GetEnvironmentsAsync(projectId);
            return Json(environments);
        }

        [HttpGet("Get/{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var env = await _environmentService.GetByIdAsync(id);
            return env == null ? NotFound() : Json(env);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] EnvironmentConfig env)
        {
            try
            {
                if (string.IsNullOrEmpty(env.Id))
                    env.Id = Guid.NewGuid().ToString();
                env.CreatedDate = DateTime.UtcNow;
                await _environmentService.SaveEnvironmentAsync(env);
                return Json(new { success = true, id = env.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create environment");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] EnvironmentConfig env)
        {
            try
            {
                await _environmentService.SaveEnvironmentAsync(env);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update environment {EnvId}", env.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _environmentService.DeleteEnvironmentAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete environment {EnvId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("SetDefault/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(string id, [FromQuery] string projectId)
        {
            try
            {
                var env = await _environmentService.GetByIdAsync(id);
                if (env == null) return NotFound();
                env.IsDefault = true;
                await _environmentService.SaveEnvironmentAsync(env);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set default environment {EnvId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
