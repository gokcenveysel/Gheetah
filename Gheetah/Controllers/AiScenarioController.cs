using System.Text;
using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Gheetah.Services.AiAdapters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    [Route("AiScenario")]
    [Authorize]
    public class AiScenarioController : Controller
    {
        private readonly IAiScenarioService _scenarioService;
        private readonly IAiProjectService _projectService;
        private readonly IAiAgentService _agentService;
        private readonly IPrePromptService _prePromptService;
        private readonly IAgentMemoryService _memoryService;
        private readonly ISessionHistoryService _sessionHistoryService;
        private readonly AiAgentAdapterFactory _adapterFactory;
        private readonly IFileService _fileService;
        private readonly IPromptValidator _validator;
        private readonly ILogger<AiScenarioController> _logger;

        public AiScenarioController(
            IAiScenarioService scenarioService,
            IAiProjectService projectService,
            IAiAgentService agentService,
            IPrePromptService prePromptService,
            IAgentMemoryService memoryService,
            ISessionHistoryService sessionHistoryService,
            AiAgentAdapterFactory adapterFactory,
            IFileService fileService,
            IPromptValidator validator,
            ILogger<AiScenarioController> logger)
        {
            _scenarioService = scenarioService;
            _projectService = projectService;
            _agentService = agentService;
            _prePromptService = prePromptService;
            _memoryService = memoryService;
            _sessionHistoryService = sessionHistoryService;
            _adapterFactory = adapterFactory;
            _fileService = fileService;
            _validator = validator;
            _logger = logger;
        }

        [HttpGet("GetScenarios")]
        public async Task<IActionResult> GetScenarios(string projectId)
        {
            if (string.IsNullOrEmpty(projectId)) return BadRequest("projectId is required");
            var scenarios = await _scenarioService.GetScenariosForProjectAsync(projectId);
            return Json(scenarios);
        }

        [HttpGet("Get/{scenarioId}")]
        public async Task<IActionResult> Get(string scenarioId, string projectId)
        {
            var scenario = await _scenarioService.GetByIdAsync(scenarioId, projectId);
            return scenario == null ? NotFound() : Json(scenario);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] AiScenario scenario)
        {
            try
            {
                if (string.IsNullOrEmpty(scenario.Id))
                    scenario.Id = Guid.NewGuid().ToString();
                scenario.CreatedDate = DateTime.UtcNow;
                scenario.CreatedBy = User.Identity?.Name;
                await _scenarioService.SaveScenarioAsync(scenario);
                return Json(new { success = true, id = scenario.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create scenario");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] AiScenario scenario)
        {
            try
            {
                await _scenarioService.SaveScenarioAsync(scenario);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update scenario {ScenarioId}", scenario.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Delete/{scenarioId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string scenarioId, [FromQuery] string projectId)
        {
            try
            {
                await _scenarioService.DeleteScenarioAsync(scenarioId, projectId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete scenario {ScenarioId}", scenarioId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Generate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate([FromBody] GenerateScenarioRequest request)
        {
            try
            {
                var project = await _projectService.GetByIdAsync(request.ProjectId);
                if (project == null) return Json(new { success = false, message = "Project not found" });

                var agent = await _agentService.GetByIdAsync(project.AiAgentId);
                if (agent == null) return Json(new { success = false, message = "AI agent not configured for this project" });

                // Load pre-prompt
                var prePrompt = string.Empty;
                if (!string.IsNullOrEmpty(project.PrePromptId))
                    prePrompt = await _prePromptService.GetRawContentAsync(project.PrePromptId) ?? string.Empty;

                // Load existing scenario titles to give agent context
                var existingScenarios = await _scenarioService.GetScenariosForProjectAsync(project.Id);
                var existingTitles = existingScenarios.Select(s => s.Title).Where(t => !string.IsNullOrEmpty(t)).ToList();

                // Load agent memory
                var memory = string.IsNullOrEmpty(project.FolderPath)
                    ? new AgentMemory { ProjectId = project.Id }
                    : await _memoryService.GetMemoryAsync(project.Id, project.FolderPath);

                // Build generation prompt
                var sb = new StringBuilder();
                sb.AppendLine(prePrompt);
                sb.AppendLine();

                if (existingTitles.Count > 0)
                {
                    sb.AppendLine("## Already Written Scenarios");
                    sb.AppendLine("Do NOT duplicate these — write NEW scenarios that cover different paths:");
                    foreach (var title in existingTitles)
                        sb.AppendLine($"- {title}");
                    sb.AppendLine();
                }

                if (memory.TestedAreas.Count > 0)
                {
                    sb.AppendLine("## Areas Already Tested");
                    sb.AppendLine(string.Join(", ", memory.TestedAreas));
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(memory.LastSessionSummary))
                {
                    sb.AppendLine("## Last Session Summary");
                    sb.AppendLine(memory.LastSessionSummary);
                    sb.AppendLine();
                }

                sb.AppendLine("## Your Task");
                sb.AppendLine($"Write a complete Gherkin scenario (Feature + Scenario + Given/When/Then steps) for:");
                sb.AppendLine(request.Topic);
                if (!string.IsNullOrEmpty(request.AdditionalContext))
                {
                    sb.AppendLine();
                    sb.AppendLine("Additional context:");
                    sb.AppendLine(request.AdditionalContext);
                }
                sb.AppendLine();
                sb.AppendLine("Return ONLY the Gherkin content — no markdown fences, no explanation, no extra text.");

                // Call the adapter
                var adapter = _adapterFactory.Create(agent.ProviderType.ToString());
                var gherkin = await adapter.GenerateAsync(agent, sb.ToString());

                // Start session history entry
                var session = new SessionHistoryEntry
                {
                    ProjectId = project.Id,
                    AgentId = agent.Id,
                    InitiatedBy = User.Identity?.Name,
                    Purpose = request.Topic,
                    Turns = new List<ConversationTurn>
                    {
                        new() { Role = "user", Content = sb.ToString() },
                        new() { Role = "assistant", Content = gherkin }
                    }
                };

                if (!string.IsNullOrEmpty(project.FolderPath))
                    await _sessionHistoryService.SaveSessionAsync(session, project.FolderPath);

                return Json(new { success = true, gherkin, sessionId = session.SessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate scenario for project {ProjectId}", request.ProjectId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("ValidateGherkin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateGherkin([FromBody] string content)
        {
            var result = await _validator.ValidateBddOutputAsync(content);
            return Json(result);
        }

        [HttpPost("ValidatePrompt")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidatePrompt([FromBody] ValidatePromptRequest request)
        {
            var context = new ValidationContext
            {
                Source = request.Source ?? "manual",
                MaxTokenBudget = 3000
            };
            var result = await _validator.ValidateInputAsync(request.Content, context);
            return Json(result);
        }

        public class ValidatePromptRequest
        {
            public string Content { get; set; }
            public string Source { get; set; }
        }
    }
}
