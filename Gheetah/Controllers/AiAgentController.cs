using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    [Route("AiAgents")]
    [Authorize(Policy = "Dynamic_admin-perm")]
    public class AiAgentController : Controller
    {
        private readonly IAiAgentService _agentService;
        private readonly ILogger<AiAgentController> _logger;

        public AiAgentController(IAiAgentService agentService, ILogger<AiAgentController> logger)
        {
            _agentService = agentService;
            _logger = logger;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            return Redirect("/SiteSettings/Index");
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var agents = await _agentService.GetAgentsAsync();
            return Json(agents);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] AiAgent agent)
        {
            try
            {
                if (string.IsNullOrEmpty(agent.Id))
                    agent.Id = Guid.NewGuid().ToString();
                agent.CreatedDate = DateTime.UtcNow;
                await _agentService.SaveAgentAsync(agent);
                return Json(new { success = true, id = agent.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create AI agent");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] AiAgent agent)
        {
            try
            {
                await _agentService.SaveAgentAsync(agent);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI agent {AgentId}", agent.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _agentService.DeleteAgentAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete AI agent {AgentId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("TestConnection/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestConnection(string id)
        {
            try
            {
                var start = DateTime.UtcNow;
                var healthy = await _agentService.TestConnectionAsync(id);
                var latencyMs = (int)(DateTime.UtcNow - start).TotalMilliseconds;
                var agent = await _agentService.GetByIdAsync(id);

                return Json(new
                {
                    success = healthy,
                    latencyMs,
                    status = agent?.LastHealthCheckStatus,
                    capabilities = agent?.Capabilities,
                    message = healthy ? "Connection successful!" : "Connection failed. Check agent configuration."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed for agent {AgentId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
