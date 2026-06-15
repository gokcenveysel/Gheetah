using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    [Route("AiExecution")]
    [Authorize]
    public class AiExecutionController : Controller
    {
        private readonly IAiExecutionService _executionService;
        private readonly IAiAgentService _agentService;
        private readonly IFileService _fileService;
        private readonly IUserService _userService;
        private readonly ILogger<AiExecutionController> _logger;

        public AiExecutionController(
            IAiExecutionService executionService,
            IAiAgentService agentService,
            IFileService fileService,
            IUserService userService,
            ILogger<AiExecutionController> logger)
        {
            _executionService = executionService;
            _agentService = agentService;
            _fileService = fileService;
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("Execute")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute([FromBody] AiExecutionRequest request)
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var users = await _fileService.LoadConfigAsync<List<Models.User>>("users.json") ?? new List<Models.User>();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                var userId = user?.Id ?? userEmail ?? "unknown";

                var agentId = request.AgentId;
                if (string.IsNullOrEmpty(agentId))
                {
                    var defaultAgent = await _agentService.GetDefaultAgentAsync();
                    agentId = defaultAgent?.Id;
                }

                if (string.IsNullOrEmpty(agentId))
                    return Json(new { success = false, message = "No AI agent configured. Please add an agent in Settings > AI Agents." });

                var sessionId = await _executionService.StartExecutionAsync(
                    request.ProjectId, request.ScenarioId, agentId, userId);

                return Json(new { success = true, sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start AI execution");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel([FromBody] string sessionId)
        {
            try
            {
                await _executionService.CancelExecutionAsync(sessionId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel execution {SessionId}", sessionId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetResult/{sessionId}")]
        public async Task<IActionResult> GetResult(string sessionId)
        {
            var result = await _executionService.GetResultAsync(sessionId);
            return result == null ? NotFound() : Json(result);
        }

        [HttpGet("GetStatus/{sessionId}")]
        public async Task<IActionResult> GetStatus(string sessionId)
        {
            var session = await _executionService.GetSessionAsync(sessionId);
            if (session == null) return NotFound();

            return Json(new
            {
                sessionId = session.SessionId,
                status = session.Status.ToString().ToLowerInvariant(),
                currentStep = session.CurrentStep,
                totalSteps = session.TotalSteps,
                isRecoverable = session.IsRecoverable
            });
        }

        [HttpGet("GetHistory/{projectId}")]
        public async Task<IActionResult> GetHistory(string projectId)
        {
            var history = await _fileService.LoadConfigAsync<List<AiExecutionResult>>("ai-execution-history.json")
                ?? new List<AiExecutionResult>();
            var filtered = history
                .Where(h => h.ProjectId == projectId)
                .OrderByDescending(h => h.StartTime)
                .Take(50)
                .ToList();
            return Json(filtered);
        }
    }

    public class AiExecutionRequest
    {
        public string ProjectId { get; set; }
        public string ScenarioId { get; set; }
        public string AgentId { get; set; }
    }
}
