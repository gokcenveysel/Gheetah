using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    [Route("AiProject")]
    [Authorize]
    public class AiProjectController : Controller
    {
        private readonly IAiProjectService _projectService;
        private readonly IAiAgentService _agentService;
        private readonly IEnvironmentService _environmentService;
        private readonly IUserService _userService;
        private readonly IPrePromptService _prePromptService;
        private readonly IFileService _fileService;
        private readonly ILogger<AiProjectController> _logger;

        public AiProjectController(
            IAiProjectService projectService,
            IAiAgentService agentService,
            IEnvironmentService environmentService,
            IUserService userService,
            IPrePromptService prePromptService,
            IFileService fileService,
            ILogger<AiProjectController> logger)
        {
            _projectService = projectService;
            _agentService = agentService;
            _environmentService = environmentService;
            _userService = userService;
            _prePromptService = prePromptService;
            _fileService = fileService;
            _logger = logger;
        }

        [HttpGet("Wizard")]
        public async Task<IActionResult> Wizard()
        {
            var agents = await _agentService.GetAgentsAsync();
            ViewBag.Agents = agents.Where(a => a.IsEnabled).ToList();
            return View();
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null) return NotFound();

            var agent = await _agentService.GetByIdAsync(project.AiAgentId);
            ViewBag.Agent = agent;
            ViewBag.ProjectId = project.Id;
            ViewBag.ProjectName = project.Name;

            var environments = await _environmentService.GetEnvironmentsAsync(id);
            ViewBag.Environments = environments;

            return View(project);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var projects = await _projectService.GetProjectsAsync();
            return Json(projects);
        }

        [HttpPost("GeneratePrePrompt")]
        [ValidateAntiForgeryToken]
        public IActionResult GeneratePrePrompt([FromBody] GeneratePrePromptRequest request)
        {
            try
            {
                var generated = _prePromptService.GenerateFromRequirements(
                    request.TestTypes, request.TargetUrl, request.Requirements ?? new());
                var tokenEstimate = generated.Length / 4;
                return Json(new { success = true, content = generated, tokenEstimate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate pre-prompt");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateAiProjectRequest request)
        {
            try
            {
                var project = request.Project;
                if (string.IsNullOrEmpty(project.Id))
                    project.Id = Guid.NewGuid().ToString();
                project.CreatedDate = DateTime.UtcNow;
                project.CreatedBy = User.Identity?.Name;

                if (request.Requirements != null && request.Requirements.Count > 0)
                {
                    var targetUrl = request.Requirements.GetValueOrDefault("targetUrl", string.Empty);
                    var generated = _prePromptService.GenerateFromRequirements(
                        project.TestTypes, targetUrl, request.Requirements);
                    var promptId = Guid.NewGuid().ToString();
                    await _prePromptService.SavePrePromptAsync(promptId, generated);
                    project.PrePromptId = promptId;
                }

                // Resolve project folder path from Site Settings
                if (string.IsNullOrEmpty(project.FolderPath))
                {
                    var baseFolder = await _fileService.LoadConfigAsync<string>("project-folder.json");
                    if (!string.IsNullOrEmpty(baseFolder))
                    {
                        var safeName = string.Concat(project.Name
                            .Split(Path.GetInvalidFileNameChars()))
                            .Replace(" ", "-");
                        project.FolderPath = Path.Combine(baseFolder, "AI-" + safeName);
                    }
                }

                await _projectService.SaveProjectAsync(project);
                return Json(new { success = true, id = project.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create AI project");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _projectService.DeleteProjectAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete AI project {ProjectId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
