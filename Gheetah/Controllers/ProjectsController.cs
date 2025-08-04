using Gheetah.Interfaces;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;
using Gheetah.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IDynamicAuthService _dynamicAuthService;
        private readonly ILogService _logService;
        private readonly IFileService _fileService;
        private readonly IEnumerable<IGitRepoService> _repoServices;
        private readonly IWebHostEnvironment _env;

        public ProjectsController(IProjectService projectService, IDynamicAuthService dynamicAuthService, ILogService logService, IFileService fileService, IEnumerable<IGitRepoService> repoServices, IWebHostEnvironment env)
        {
            _projectService = projectService;
            _dynamicAuthService = dynamicAuthService;
            _logService = logService;
            _fileService = fileService;
            _repoServices = repoServices;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var logs = await _logService.GetLogsAsync();
            ViewBag.Logs = logs
                .Where(l => l.Action.Contains("Project"))
                .OrderByDescending(l => l.Timestamp)
                .ToList();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ProjectList(bool showToast = false)
        {
            var projects = await _projectService.GetProjectsAsync();
    
            if (showToast && TempData["Success"] == null)
            {
                TempData.Keep("Success");
            }
    
            return View(projects);
        }

        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> ManageProjects()
        {
            try
            {
                var providerTask = _dynamicAuthService.GetConfiguredProviderAsync();
                var repoSettingsTask = _fileService.LoadConfigAsync<List<RepoSettingsVm>>("remote-repos-settings.json");
                
                await Task.WhenAll(providerTask, repoSettingsTask);
        
                ViewBag.Provider = await providerTask;
                var repoSettings = await repoSettingsTask ?? new();

                var allRepos = new Dictionary<string, List<GitRepoVm>>();
                var tasks = repoSettings.Select(repo => LoadReposAsync(repo, allRepos));
                await Task.WhenAll(tasks);

                ViewBag.AllRepos = allRepos;
                return View(repoSettings);
            }
            catch
            {
                return View(new List<RepoSettingsVm>());
            }
        }

        private async Task LoadReposAsync(RepoSettingsVm repo, Dictionary<string, List<GitRepoVm>> allRepos)
        {
            var service = _repoServices.FirstOrDefault(s => s.IsMatch(repo.RepoType));
            var userEmail = User?.Identity?.Name ?? "UnknownUser";
            if (service != null)
            {
                try
                {
                    allRepos[repo.Id] = await service.GetReposAsync(repo);
                }
                catch (Exception ex)
                {
                    await _logService.LogAsync(
                        userEmail,
                        "Repo Fetch Error",
                        $"Error occurred while pulling repo for type: [{repo.RepoType}] ID: {repo.Id}, Hata: {ex.Message}"
                    );
                    allRepos[repo.Id] = new List<GitRepoVm>();
                }
            }
        }

        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        [HttpPost("ClonePublicGitHubRepo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClonePublicGitHubRepo(string repoId, string repoUrl)
        {
            var allSettings = await _fileService.LoadConfigAsync<List<RepoSettingsVm>>("remote-repos-settings.json") ?? new();
            
            var repoInfo = allSettings.FirstOrDefault(x => x.Id == repoId);

            if (repoInfo == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Repository settings not found. Please check your configuration." });
                }
                TempData["Error"] = "Repository settings not found. Please check your configuration.";
                return RedirectToAction("ManageProjects", new { repoId });
            }

            if (!repoUrl.EndsWith(".git"))
            {
                repoUrl = repoUrl.EndsWith("/") ? repoUrl + ".git" : repoUrl + ".git";
            }

            if (!repoUrl.StartsWith("https://github.com/"))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Invalid GitHub repository URL. URL must start with 'https://github.com/'" });
                }
                TempData["Error"] = "Invalid GitHub repository URL. URL must start with 'https://github.com/'";
                return RedirectToAction("ManageProjects", new { repoId });
            }

            var uri = new Uri(repoUrl);
            var segments = uri.Segments;
            var repoDisplayName = segments.Length >= 3 ? $"{segments[1]}{segments[2]}".Trim('/') : "Unknown";

            var projectFolder = await _fileService.LoadConfigAsync<string>("project-folder.json")
                                ?? Path.Combine(_env.ContentRootPath, "ClonedProjects");

            try
            {
                var cloneTask = _projectService.CloneProjectAsync(repoUrl, repoInfo, "Unknown", projectFolder);
                
                var timeoutTask = Task.Delay(270000);
                var completedTask = await Task.WhenAny(cloneTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    await _logService.LogAsync(User.Identity.Name, "Public Repo Clone", $"TIMEOUT: Clone timeout for {repoUrl}");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Clone operation timed out. Please try again." });
                    }
                    TempData["Error"] = "Clone operation timed out. Please try again.";
                    return RedirectToAction("ManageProjects", new { repoId });
                }

                await cloneTask;
                
                await _logService.LogAsync(User.Identity.Name, "Public Repo Clone", $"SUCCESS: Cloned public repository from {repoUrl}");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = true, 
                        message = $"Public repository successfully cloned: {repoDisplayName}",
                        redirectUrl = Url.Action("ProjectList") 
                    });
                }
                
                TempData["Success"] = $"Public repository successfully cloned: {repoDisplayName}";
                return RedirectToAction("ProjectList");
            }
            catch (Exception ex)
            {
                await _logService.LogAsync(User.Identity.Name, "Public Repo Clone", $"FAILED: Clone failed for {repoUrl} - Reason: {ex.Message}");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"Clone operation failed: {ex.Message}" });
                }
                
                TempData["Error"] = $"Clone operation failed: {ex.Message}";
                return RedirectToAction("ManageProjects", new { repoId });
            }
        }
        
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloneProject(string repoId, string repoUrl, string repoDisplayName, string repoLanguage)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Unauthorized(new 
                    {
                        success = false,
                        message = "Authentication required",
                        redirectUrl = Url.Action("Login", "Account")
                    });
                }

                var allSettings = await _fileService.LoadConfigAsync<List<RepoSettingsVm>>("remote-repos-settings.json") ?? new();
                var repoInfo = allSettings.FirstOrDefault(x => x.Username != null && !string.IsNullOrEmpty(x.AccessToken));

                if (repoInfo == null)
                {
                    return BadRequest(new 
                    {
                        success = false,
                        message = "Repository credentials not found. Please configure a connection with a valid access token."
                    });
                }

                repoInfo.DisplayName = repoDisplayName;
                var projectFolder = await _fileService.LoadConfigAsync<string>("project-folder.json")
                                    ?? Path.Combine(_env.ContentRootPath, "Projects");

                var cloneTask = _projectService.CloneProjectAsync(repoUrl, repoInfo, repoLanguage, projectFolder);
                var timeoutTask = Task.Delay(270000);
                var completedTask = await Task.WhenAny(cloneTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    await _logService.LogAsync(User.Identity.Name, "Project Clone", $"TIMEOUT: Clone timeout for {repoUrl}");
                    return BadRequest(new 
                    {
                        success = false,
                        message = "Clone operation timed out. Please try again."
                    });
                }

                await cloneTask;
                await _logService.LogAsync(User.Identity.Name, "Project Clone", $"SUCCESS: Cloned project from {repoUrl}");

                return Ok(new 
                {
                    success = true,
                    message = $"Project successfully cloned: {repoDisplayName}",
                    redirectUrl = Url.Action("ProjectList")
                });
            }
            catch (Exception ex)
            {
                await _logService.LogAsync(User.Identity.Name, "Project Clone", $"FAILED: Clone failed for {repoUrl} - Reason: {ex.Message}");
                return StatusCode(500, new 
                {
                    success = false,
                    message = $"Clone operation failed: {ex.Message}",
                    redirectUrl = Url.Action("ManageProjects", new { repoId })
                });
            }
        }

        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> UploadProject(IFormFile archiveFile, string language)
        {
            object ErrorResponse(string message) => new {
                success = false,
                message = message,
                redirectUrl = Url.Action("ManageProjects")
            };

            try
            {
                if (archiveFile == null || archiveFile.Length == 0)
                    return Json(ErrorResponse("Please select a file to upload"));

                if (archiveFile.Length > 50 * 1024 * 1024)
                    return Json(ErrorResponse("File size exceeds 50MB"));

                var extension = Path.GetExtension(archiveFile.FileName).ToLower();
                if (!new[] { ".zip", ".rar", ".7z" }.Contains(extension))
                    return Json(ErrorResponse("Only .zip, .rar and .7z files are accepted"));

                var projectFolder = await _fileService.LoadConfigAsync<string>("project-folder.json")
                                    ?? Path.Combine(_env.ContentRootPath, "ClonedProjects");

                await _projectService.UploadLocalProjectAsync(archiveFile, language, projectFolder);

                return Json(new {
                    success = true,
                    message = "Project uploaded successfully!",
                    redirectUrl = Url.Action("ProjectList")
                });
            }
            catch (Exception ex)
            {
                var cleanError = ex.Message.Replace(Environment.NewLine, " ");
                return Json(ErrorResponse($"Installation error: {cleanError}"));
            }
        }

        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuildProject([FromBody] BuildProjectRequest request)
        {
            var result = await _projectService.BuildProjectAsync(request.ProjectId, request.LanguageType);
            return Json(new
            {
                isSuccess = result.IsSuccess,
                message = result.Message
            });
        }
        
        [Authorize(Policy = "Dynamic_admin-perm")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(string projectId)
        {
            try
            {
                if (await _projectService.IsProjectLockedAsync(projectId))
                {
                    return Json(new { 
                        success = false,
                        message = "Project cannot be deleted while it is being executed. Please try again later."
                    });
                }

                await _projectService.DeleteProjectAsync(projectId);
                return Json(new { success = true, message = "Project deleted successfully" });
            }

            catch (Exception ex)
            {
                return Json(new { 
                    success = false,
                    message = ex.Message.StartsWith("Delete failed:") 
                        ? ex.Message.Replace("Delete failed:", "").Trim()
                        : "Delete operation partially completed. Contact administrator."
                });
            }
        }
        
    }
}
