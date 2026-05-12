using Gheetah.Interfaces;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;
using Gheetah.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
        private readonly string _rootPath = Directory.GetCurrentDirectory();

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
            if (archiveFile == null || archiveFile.Length == 0)
                return Json(new { success = false, message = "Please select a file to upload" });

            if (archiveFile.Length > 50 * 1024 * 1024)
                return Json(new { success = false, message = "File size exceeds 50MB" });

            var extension = Path.GetExtension(archiveFile.FileName).ToLower();
            if (!new[] { ".zip", ".rar", ".7z" }.Contains(extension))
                return Json(new { success = false, message = "Only .zip, .rar and .7z files are accepted" });

            try
            {
                var projectFolder = await _fileService.LoadConfigAsync<string>("project-folder.json")
                                    ?? Path.Combine(_env.ContentRootPath, "ClonedProjects");

                await _projectService.UploadLocalProjectAsync(archiveFile, language, projectFolder);

                string folderName = Path.GetFileNameWithoutExtension(archiveFile.FileName);
                string uploadedPath = Path.Combine(projectFolder, folderName);

                if (Directory.Exists(uploadedPath) && !LibGit2Sharp.Repository.IsValid(uploadedPath))
                {
                    InitializeGitRepository(uploadedPath, folderName);
                }

                return Json(new {
                    success = true,
                    message = "Project uploaded and Git initialized successfully!",
                    redirectUrl = Url.Action("ProjectList")
                });
            }
            catch (Exception ex)
            {
                var cleanError = ex.Message.Replace(Environment.NewLine, " ");
                return Json(new { success = false, message = $"Installation error: {cleanError}" });
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

        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        [HttpGet]
        public IActionResult CreateProject()
        {
            return View();
        }

        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        [HttpPost]
        public async Task<IActionResult> GenerateProject([FromBody] ProjectCreateViewModel model)
        {
            try
            {
                string rootDir = _env.ContentRootPath;
                string dataFolderPath = Path.Combine(rootDir, "Data");
                string configFilePath = Path.Combine(dataFolderPath, "project-folder.json");

                if (!System.IO.File.Exists(configFilePath))
                {
                    return Json(new { success = false, type = "config_error", message = "Configuration file not found." });
                }

                var configContent = (await System.IO.File.ReadAllTextAsync(configFilePath)).Trim();
                string targetBaseDir = string.Empty;

                if (configContent.StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(configContent);
                    if (doc.RootElement.TryGetProperty("ProjectFolderPath", out var pathProp))
                    {
                        targetBaseDir = pathProp.GetString();
                    }
                }
                else
                {
                    targetBaseDir = configContent.Replace("\"", "");
                }

                if (string.IsNullOrEmpty(targetBaseDir) || !Directory.Exists(targetBaseDir))
                {
                    return Json(new { success = false, type = "config_error", message = $"Target directory invalid or not found: {targetBaseDir}" });
                }

                string projectFolderPath = Path.Combine(targetBaseDir, model.ProjectName);
                if (Directory.Exists(projectFolderPath))
                {
                    return Json(new { success = false, message = "A folder with this name already exists!" });
                }

                string sourcePath = Path.Combine(rootDir, "Templates", model.Language, $"Base_{model.ProjectType}");
                CopyAndProcessFiles(sourcePath, projectFolderPath, model);
                ProcessAddons(model, projectFolderPath, rootDir);
                HandlePackageManagement(projectFolderPath, model);
                
                await UpdateProjectsJson(model, projectFolderPath, rootDir);

                InitializeGitRepository(projectFolderPath, model.ProjectName);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Backend Error: " + ex.Message });
            }
        }

        private void CopyAndProcessFiles(string sourceDir, string targetDir, ProjectCreateViewModel model)
        {
            Directory.CreateDirectory(targetDir);

            string rootDir = _env.ContentRootPath;
            string mapPath = Path.Combine(rootDir, "Templates", model.Language, "dependency_map.json");
            string adapterDeps = "";
            string addonDeps = "";

            if (System.IO.File.Exists(mapPath))
            {
                var mapJson = System.IO.File.ReadAllText(mapPath);
                using var doc = JsonDocument.Parse(mapJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("Adapters", out var adapters) && 
                    adapters.TryGetProperty(model.TestAdapter, out var adapterValue))
                {
                    adapterDeps = adapterValue.GetString();
                }

                if (root.TryGetProperty("Addons", out var addons))
                {
                    foreach (var selectedAddon in model.Addons ?? new List<string>())
                    {
                        string addonKey = selectedAddon == "DB" ? "Database" : selectedAddon;
                        
                        if (addons.TryGetProperty(addonKey, out var addonValue))
                        {
                            addonDeps += addonValue.GetString() + "\n";
                        }
                    }
                }
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                
                if (model.Language == "C#" && fileName.EndsWith(".csproj"))
                    fileName = $"{model.ProjectName}.csproj";

                string destFile = Path.Combine(targetDir, fileName);
                string content = System.IO.File.ReadAllText(file);

                content = content.Replace("{{ProjectName}}", model.ProjectName)
                                 .Replace("{{TestAdapter}}", model.TestAdapter)
                                 .Replace("{{AdapterDependencies}}", adapterDeps)
                                 .Replace("{{AddonDependencies}}", addonDeps);

                System.IO.File.WriteAllText(destFile, content);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);
                if (dirName == "{{ProjectName}}") dirName = model.ProjectName;
                CopyAndProcessFiles(directory, Path.Combine(targetDir, dirName), model);
            }
        }

        private void ProcessAddons(ProjectCreateViewModel model, string projectPath, string rootDir)
        {
            if (model.Addons == null || !model.Addons.Any()) return;

            string extension = model.Language == "C#" ? "cs" : "java";
            string stepFolder = model.Language == "C#" 
                ? Path.Combine(projectPath, "StepDefinitions") 
                : Path.Combine(projectPath, "src", "test", "java", model.ProjectName, "stepdefinitions");

            string addonSourceBase = Path.Combine(rootDir, "Templates", model.Language, "Addons");

            foreach (var addon in model.Addons)
            {
                string addonFileName = addon == "API" ? $"ApiSteps.{extension}" : $"DbSteps.{extension}";
                string sourceFile = Path.Combine(addonSourceBase, addonFileName);

                if (System.IO.File.Exists(sourceFile))
                {
                    if (!Directory.Exists(stepFolder)) Directory.CreateDirectory(stepFolder);
            
                    string content = System.IO.File.ReadAllText(sourceFile).Replace("{{ProjectName}}", model.ProjectName);
                    System.IO.File.WriteAllText(Path.Combine(stepFolder, addonFileName), content);
                }
            }
        }

        private async Task UpdateProjectsJson(ProjectCreateViewModel model, string fullPath, string rootDir)
        {
            string jsonPath = Path.Combine(rootDir, "Data", "projects.json");
            List<Project> projects = new List<Project>();

            if (System.IO.File.Exists(jsonPath))
            {
                var existingJson = await System.IO.File.ReadAllTextAsync(jsonPath);
                projects = JsonSerializer.Deserialize<List<Project>>(existingJson) ?? new List<Project>();
            }

            string buildFileName = model.Language == "C#" ? $"{model.ProjectName}.csproj" : "pom.xml";

            var newProject = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.ProjectName,
                RepoUrl = "Local Project",
                LanguageType = model.Language,
                UserId = User.Identity?.Name ?? "SYSTEM",
                IsBuilt = false,
                ClonedDate = DateTime.Now,
                FeatureFileCount = 1,
                ScenarioCount = 1,
                ProjectInfos = new List<ProjectInfo>
                {
                    new ProjectInfo
                    {
                        ProjectName = model.ProjectName,
                        BuildInfoFileName = buildFileName,
                        BuildInfoFileFullPath = Path.Combine(fullPath, buildFileName),
                        FeatureFilesPath = model.Language == "C#" 
                            ? Path.Combine(fullPath, "Features") 
                            : Path.Combine(fullPath, "src/test/resources/features"),
                        BuildedTestFileName = "Not Built Yet",
                        BuildedTestFileFullPath = "Pending",
                        Scenarios = new List<FeatureScenarioInfo>() 
                    }
                }
            };

            projects.Add(newProject);
    
            var options = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(projects, options));
        }

        private void HandlePackageManagement(string targetDir, ProjectCreateViewModel model)
        {
            if (model.Language == "C#")
            {
                string sourceUrl = string.IsNullOrWhiteSpace(model.CustomSourceUrl) 
                    ? "https://api.nuget.org/v3/index.json" 
                    : model.CustomSourceUrl;

                string nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""GheetahSource"" value=""{sourceUrl}"" />
  </packageSources>
</configuration>";

                System.IO.File.WriteAllText(Path.Combine(targetDir, "nuget.config"), nugetConfig);
            }
            else if (model.Language == "Java")
            {
                string pomPath = Path.Combine(targetDir, "pom.xml");
                if (System.IO.File.Exists(pomPath))
                {
                    string customRepoXml = "";
                    if (!string.IsNullOrWhiteSpace(model.CustomSourceUrl))
                    {
                        customRepoXml = $@"
    <repositories>
        <repository>
            <id>gheetah-custom-repo</id>
            <url>{model.CustomSourceUrl}</url>
        </repository>
    </repositories>";
                    }

                    string content = System.IO.File.ReadAllText(pomPath);
                    content = content.Replace("{{CustomRepo}}", customRepoXml);
                    System.IO.File.WriteAllText(pomPath, content);
                }
            }
        }
        
        private void InitializeGitRepository(string path, string projectName)
        {
            var user = User.Identity?.Name ?? "system@gheetah.com";
            try
            {
                string gitIgnorePath = Path.Combine(path, ".gitignore");
                if (!System.IO.File.Exists(gitIgnorePath))
                {
                    string ignoreContent = @"
bin/
obj/
.vs/
*.user
*.userosscache
*.sln.docstates
.DS_Store";
                    System.IO.File.WriteAllText(gitIgnorePath, ignoreContent);
                }

                LibGit2Sharp.Repository.Init(path);

                using (var repo = new LibGit2Sharp.Repository(path))
                {
                    LibGit2Sharp.Commands.Stage(repo, "*");

                    var author = new Signature(user, user, DateTimeOffset.Now);
                    repo.Commit($"Gheetah IDE: Initial repository setup for {projectName}", author, author);
                }
            }
            catch (Exception ex)
            {
                _logService.LogAsync(User.Identity?.Name ?? "SYSTEM", "Git Init Error", $"Project: {projectName}, Error: {ex.Message}");
            }
        }
    }
}
