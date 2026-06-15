using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    [Route("GitConflict")]
    [Authorize]
    public class GitConflictController : Controller
    {
        private readonly IGitConflictService _conflictService;
        private readonly ILogger<GitConflictController> _logger;

        public GitConflictController(IGitConflictService conflictService, ILogger<GitConflictController> logger)
        {
            _conflictService = conflictService;
            _logger = logger;
        }

        [HttpGet("Check")]
        public async Task<IActionResult> Check([FromQuery] string repoPath)
        {
            var hasConflicts = await _conflictService.HasConflictsAsync(repoPath);
            var files = hasConflicts ? await _conflictService.GetConflictedFilesAsync(repoPath) : new List<string>();
            return Json(new { hasConflicts, files });
        }

        [HttpGet("ParseFile")]
        public async Task<IActionResult> ParseFile([FromQuery] string filePath)
        {
            var blocks = await _conflictService.ParseConflictsAsync(filePath);
            return Json(blocks);
        }

        [HttpPost("ApplyResolutions")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyResolutions([FromBody] ApplyResolutionsRequest request)
        {
            try
            {
                await _conflictService.ApplyResolutionsAsync(request.RepoPath, request.FilePath, request.Resolutions);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply conflict resolutions for {FilePath}", request?.FilePath);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("CommitResolution")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CommitResolution([FromBody] CommitResolutionRequest request)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Gheetah";
                await _conflictService.CommitResolutionAsync(request.RepoPath, request.CommitMessage, userName, userName + "@gheetah");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit resolution in {RepoPath}", request?.RepoPath);
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class ApplyResolutionsRequest
        {
            public string RepoPath { get; set; }
            public string FilePath { get; set; }
            public List<ResolvedBlock> Resolutions { get; set; } = new();
        }

        public class CommitResolutionRequest
        {
            public string RepoPath { get; set; }
            public string CommitMessage { get; set; }
        }
    }
}
