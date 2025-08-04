using Gheetah.Helper;
using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.AgentModel;
using Gheetah.Models.ProcessModel;
using Gheetah.Services;
using Gherkin;
using Gherkin.Ast;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.IO.Compression;

namespace Gheetah.Controllers;

public class ScenariosController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ILogService _logService;
    private readonly IFileService _fileService;
    private readonly IHubContext<GheetahHub> _hubContext;
    private readonly IProcessService _processService;
    private readonly IUserService _userService;
    private readonly IScenarioProcessor _scenarioProcessor;

    public ScenariosController(IProjectService projectService, ILogService logService, IFileService fileService, IHubContext<GheetahHub> hubContext, IProcessService processService, IUserService userService, IScenarioProcessor scenarioProcessor)
    {
        _projectService = projectService;
        _logService = logService;
        _fileService = fileService;
        _hubContext = hubContext;
        _processService = processService;
        _userService = userService;
        _scenarioProcessor = scenarioProcessor;
    }

    [HttpGet]
    [Route("Scenarios/ScenarioDetail/{id}")]
    public async Task<IActionResult> ScenarioDetail(Guid id)
    {
        var projects = await _projectService.GetProjectsAsync();
        var project = projects.FirstOrDefault(p => p.Id == id.ToString());
        if (project == null)
            return NotFound("The project not found!");

        var mainProjectInfo = project.ProjectInfos.FirstOrDefault();

        ViewBag.ProjectPath = mainProjectInfo.BuildedTestFileFullPath;
        ViewBag.ProjectName = project.Name;
        ViewBag.FeatureFileCount = project.FeatureFileCount;
        ViewBag.ScenarioCount = project.ScenarioCount;

        return View();
    }

    [HttpGet]
    public IActionResult ExecutionAllTestOutput(string processId)
    {
        var process = _processService.GetProcess(processId);
        if (process == null)
            return NotFound();

        ViewBag.HtmlReport = process.HtmlReport;
        return View(process);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetFeatureFiles(Guid id)
    {
        var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
        if (string.IsNullOrWhiteSpace(clonesRoot) || !Directory.Exists(clonesRoot))
        {
            await _logService.LogAsync(User.Identity.Name, "Test Explorer", "Project folder config not found or invalid.");
            return NotFound("Project folder config not found!");
        }

        var projects = await _projectService.GetProjectsAsync();
        var project = projects.FirstOrDefault(p => p.Id == id.ToString());
        if (project == null)
        {
            await _logService.LogAsync(User.Identity.Name, "Test Explorer", $"Project not found for ID: {id}");
            return NotFound("The project not found!");
        }

        await _logService.LogAsync(User.Identity.Name, "Test Explorer", 
            $"Project: {project.Name}, Language: {project.LanguageType}, ProjectInfos: {string.Join(", ", project.ProjectInfos.Select(p => p.FeatureFilesPath ?? "null"))}");

        var infoWithFeatures = project.ProjectInfos?.FirstOrDefault(p => 
            !string.IsNullOrWhiteSpace(p.FeatureFilesPath) &&
            Directory.Exists(p.FeatureFilesPath) &&
            (project.LanguageType.ToLower() != "java" || !p.FeatureFilesPath.Contains("target", StringComparison.OrdinalIgnoreCase)));
        
        if (infoWithFeatures == null)
        {
            await _logService.LogAsync(User.Identity.Name, "Test Explorer", "No valid FeatureFilesPath found.");
            return NotFound("There is no project that including feature files");
        }

        await _logService.LogAsync(User.Identity.Name, "Test Explorer", 
            $"Selected FeatureFilesPath: {infoWithFeatures.FeatureFilesPath}");

        var searchOption = project.LanguageType.ToLower() == "java" ? SearchOption.AllDirectories : SearchOption.AllDirectories;
        var featureFiles = Directory.GetFiles(infoWithFeatures.FeatureFilesPath, "*.feature", searchOption)
            .Where(file => !file.Contains("target", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (featureFiles.Length == 0)
        {
            await _logService.LogAsync(User.Identity.Name, "Test Explorer", 
                $"No .feature files found in {infoWithFeatures.FeatureFilesPath}. Checking subdirectories: {Directory.EnumerateFiles(infoWithFeatures.FeatureFilesPath, "*.feature", SearchOption.AllDirectories).Any()}");
            return NotFound("There is no .feature file!");
        }

        await _logService.LogAsync(User.Identity.Name, "Test Explorer", 
            $"Found feature files: {string.Join(", ", featureFiles)}");

        var projectName = project.Name;
        var languageType = project.LanguageType;

        List<string> testCases = new List<string>();
        try
        {
            if (project.LanguageType.ToLower() == "java")
            {
                if (Directory.Exists(infoWithFeatures.FeatureFilesPath))
                {
                    testCases.AddRange(ScenarioHelper.ListAllTests(infoWithFeatures.FeatureFilesPath, SearchOption.AllDirectories)
                        .Where(tc => !tc.Contains("target", StringComparison.OrdinalIgnoreCase)));
                }
            }
            else
            {
                foreach (var projectInfo in project.ProjectInfos)
                {
                    var featureFilesPath = projectInfo.FeatureFilesPath;
                    if (Directory.Exists(featureFilesPath) && 
                        !featureFilesPath.Contains("target", StringComparison.OrdinalIgnoreCase))
                    {
                        testCases.AddRange(ScenarioHelper.ListAllTests(featureFilesPath, SearchOption.AllDirectories));
                    }
                }
            }

            await _logService.LogAsync(User.Identity.Name, "Test Explorer", 
                $"Test cases before Distinct: {testCases.Count}, {string.Join(", ", testCases)}");

            testCases = testCases
                .Select(tc => tc.Contains('#') ? tc.Split('#').Last().Trim() : tc.Trim())
                .Distinct()
                .Select(uniqueName => testCases.FirstOrDefault(tc => 
                    (tc.Contains('#') ? tc.Split('#').Last().Trim() : tc.Trim()) == uniqueName))
                .Where(tc => tc != null)
                .ToList();

            await _logService.LogAsync(User.Identity.Name, "Test Explorer", 
                $"Test cases after Distinct: {testCases.Count}, {string.Join(", ", testCases)}");
        }
        catch (Exception ex)
        {
            await _logService.LogAsync(User.Identity.Name, "Test Explorer",
                $"FAILED: Test explorer is failed: {ex.Message}");
            return StatusCode(500, $"Test explorer failed: {ex.Message}");
        }

        await _logService.LogAsync(User.Identity.Name, "Test Explorer", 
            $"Test cases before ProcessFeatureFiles: {testCases.Count}, {string.Join(", ", testCases)}");

        var projectPath = Path.Combine(clonesRoot, project.Name);
        var processedFiles = ScenarioHelper.ProcessFeatureFiles(projectPath, projectName, languageType, testCases);

        if (processedFiles != null)
        {
            await _logService.LogAsync(User.Identity.Name, "Test Explorer", 
                $"Processed files after ProcessFeatureFiles (raw): {processedFiles?.GetType().Name}, Count: {(processedFiles is IEnumerable<object> files ? files.Cast<object>().Count() : 0)}, {string.Join(", ", processedFiles is IEnumerable<object> filesList ? filesList.Cast<object>() : new[] { processedFiles })}");
        }

        if (processedFiles != null && processedFiles is IEnumerable<object> processedItems)
        {
            var uniqueFiles = processedItems
                .Cast<object>() 
                .Select(f => 
                {
                    var nameProp = f.GetType().GetProperty("Name")?.GetValue(f)?.ToString() ?? 
                                   f.GetType().GetProperty("Scenario")?.GetValue(f)?.ToString() ?? 
                                   f.ToString().Trim();
                    return nameProp;
                })
                .Distinct()
                .Select(uniqueName => processedItems.Cast<object>().FirstOrDefault(f => 
                    (f.GetType().GetProperty("Name")?.GetValue(f)?.ToString() ?? 
                     f.GetType().GetProperty("Scenario")?.GetValue(f)?.ToString() ?? 
                     f.ToString().Trim()) == uniqueName))
                .Where(f => f != null)
                .ToList();
            processedFiles = uniqueFiles;
        }

        return Json(processedFiles);
    }

    [HttpGet]
    public IActionResult GetFeatureFileContent(string filePath)
    {
        if (!Directory.Exists(filePath)) return Content("Invalid file path");
        return Content(System.IO.File.ReadAllText(filePath), "text/plain");
    }

    [HttpGet]
    public async Task<IActionResult> GetScenarioContent(string filePath, string scenarioName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BadRequest("File path is missing.");
            }
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                return BadRequest("Scenario name is missing.");
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Scenario file not found.");
            }

            var content = System.IO.File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(content))
            {
                return NotFound("Scenario file is empty.");
            }

            var parser = new Parser();
            using var reader = new StringReader(content);
            var gherkinDocument = parser.Parse(reader);
            if (gherkinDocument == null || gherkinDocument.Feature == null)
            {
                return Content("Invalid Gherkin document.", "text/plain");
            }

            var feature = gherkinDocument.Feature;
            var scenario = feature.Children.OfType<Scenario>()
                .FirstOrDefault(s => s.Name == scenarioName);

            if (scenario == null)
            {
                return Content("Scenario not found.", "text/plain");
            }

            var fullContent = $"{ScenarioHelper.GetFeatureHeader(feature)}\n" +
                              $"{ScenarioHelper.GetBackgroundText(feature)}\n" +
                              $"{ScenarioHelper.GetScenarioText(scenario)}";

            return Content(fullContent, "text/plain");
        }
        catch (Exception ex)
        {
            await _logService.LogAsync(User.Identity.Name, "GetScenarioContent", $"FAILED: {ex.Message}");
            return Content("Error: Scenario failed to load", "text/plain");
        }
    }

    [HttpPost]
    public async Task<IActionResult> RunScenario([FromBody] Gheetah.Models.ScenarioModel.RunScenarioRequest request)
    {
        try
        {
            await _projectService.LockProjectAsync(request.projectId.ToString(), User.Identity.Name);
            var user = await _userService.GetUserByEmail(User.Identity.Name);

            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == request.projectId.ToString());
            if (project == null)
            {
                await _logService.LogAsync(User.Identity.Name, "RunScenario",
                    $"FAILED: ProjectId {request.projectId} not found.");
                return NotFound("The project not found!");
            }

            string processId = Guid.NewGuid().ToString();

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            if (string.IsNullOrWhiteSpace(clonesRoot) || !Directory.Exists(clonesRoot))
            {
                await _logService.LogAsync(User.Identity.Name, "RunScenario",
                    $"FAILED: Project folder config not found.");
                return BadRequest("Project folder config not found.");
            }

            var projectInfo = project.ProjectInfos?.FirstOrDefault(pi =>
                pi.Scenarios != null && pi.Scenarios.Any() && !string.IsNullOrEmpty(pi.BuildedTestFileName));
            if (projectInfo == null)
            {
                await _logService.LogAsync(User.Identity.Name, "RunScenario",
                    $"FAILED: No ProjectInfo with scenarios and built test file found for project {project.Name}.");
                return BadRequest(
                    $"No ProjectInfo with scenarios and built test file found for project {project.Name}.");
            }

            var projectPath = Path.Combine(clonesRoot, projectInfo.ProjectName);
            if (!Directory.Exists(projectPath))
            {
                await _logService.LogAsync(User.Identity.Name, "RunScenario",
                    $"FAILED: Project path {projectPath} does not exist.");
                return BadRequest($"Project path {projectPath} does not exist.");
            }

            if (!string.IsNullOrEmpty(request.AgentId))
            {
                var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json");
                var agent = agents?.FirstOrDefault(a =>
                    a.AgentId == request.AgentId && a.Status == "online" && a.Availability == "available");
                if (agent == null || string.IsNullOrEmpty(agent.ConnectionId))
                {
                    await _logService.LogAsync(User.Identity.Name, "RunScenario",
                        $"FAILED: AgentId {request.AgentId} not found or offline.");
                    return Json(new { success = false, message = $"Agent {request.AgentId} not found or offline." });
                }

                var tempPath = Path.Combine(Path.GetTempPath(), $"{projectInfo.ProjectName}_{processId}.zip");
                try
                {
                    string[] files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories);
                    await _logService.LogAsync(User.Identity.Name, "RunScenario",
                        $"Project files before zipping: {string.Join(", ", files)}");

                    ZipFile.CreateFromDirectory(projectPath, tempPath);
                    byte[] zipData = await System.IO.File.ReadAllBytesAsync(tempPath);
                    await _logService.LogAsync(User.Identity.Name, "RunScenario",
                        $"Zip created: {tempPath}, Size: {zipData.Length} bytes");

                    await _hubContext.Clients.Client(agent.ConnectionId)
                        .SendAsync("SendZipFile", request.AgentId, processId, zipData);
                    await _logService.LogAsync(User.Identity.Name, "RunScenario",
                        $"Zip sent: AgentId: {request.AgentId}, ConnectionId: {agent.ConnectionId}, ProcessId: {processId}, ScenarioTag: {request.ScenarioTag}");

                    await _hubContext.Clients.Client(agent.ConnectionId).SendAsync("ExecuteScenario", processId,
                        request.ScenarioTag, project.LanguageType, projectInfo.BuildedTestFileName);
                    await _logService.LogAsync(User.Identity.Name, "RunScenario",
                        $"ExecuteScenario called: AgentId: {request.AgentId}, ProcessId: {processId}, ScenarioTag: {request.ScenarioTag}, LanguageType: {project.LanguageType}, BuildedTestFileName: {projectInfo.BuildedTestFileName}");

                    System.IO.File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    await _logService.LogAsync(User.Identity.Name, "RunScenario",
                        $"FAILED: Zip creation/sending error: {ex.Message}");
                    return Json(new { success = false, message = $"Zip creation/sending error: {ex.Message}" });
                }
            }
            else
            {
                processId = _scenarioProcessor.StartScenario(user.Id, project, request);
                await _logService.LogAsync(User.Identity.Name, "RunScenario",
                    $"Local execution started: ProcessId: {processId}, ScenarioTag: {request.ScenarioTag}");
            }

            return Json(new { success = true, processId });
        }
        catch (Exception ex)
        {
            await _logService.LogAsync(User.Identity.Name, "RunScenario", $"FAILED: {ex.Message}");
            return Json(new { success = false, message = ex.Message });
        }
        finally
        {
            if (await _projectService.IsProjectLockedAsync(request.projectId.ToString()))
            {
                await _projectService.UnlockProjectAsync(request.projectId.ToString());
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> RunAllScenarios([FromBody] Models.ScenarioModel.RunAllScenariosRequest request)
    {
        try
        {
            await _projectService.LockProjectAsync(request.projectId, User.Identity.Name);
            var user = await _userService.GetUserByEmail(User.Identity.Name);

            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == request.projectId.ToString());
            if (project == null)
            {
                await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"FAILED: ProjectId {request.projectId} not found.");
                return NotFound("The project not found!");
            }

            string processId = Guid.NewGuid().ToString();

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            if (string.IsNullOrWhiteSpace(clonesRoot) || !Directory.Exists(clonesRoot))
            {
                await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"FAILED: Project folder config not found.");
                return BadRequest("Project folder config not found.");
            }

            var projectInfo = project.ProjectInfos?.FirstOrDefault(pi => pi.Scenarios != null && pi.Scenarios.Any());
            if (projectInfo == null)
            {
                await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"FAILED: No ProjectInfo with scenarios found for project {project.Name}.");
                return BadRequest($"No ProjectInfo with scenarios found for project {project.Name}.");
            }

            var projectPath = Path.Combine(clonesRoot, projectInfo.ProjectName);
            if (!Directory.Exists(projectPath))
            {
                await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"FAILED: Project path {projectPath} does not exist.");
                return BadRequest($"Project path {projectPath} does not exist.");
            }

            if (!string.IsNullOrEmpty(request.AgentId))
            {
                var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json");
                var agent = agents?.FirstOrDefault(a => a.AgentId == request.AgentId && a.Status == "online" && a.Availability == "available");
                if (agent == null || string.IsNullOrEmpty(agent.ConnectionId))
                {
                    await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"FAILED: AgentId {request.AgentId} not found or offline.");
                    return Json(new { success = false, message = $"Agent {request.AgentId} not found or offline." });
                }

                var tempPath = Path.Combine(Path.GetTempPath(), $"{projectInfo.ProjectName}_{processId}.zip");
                try
                {
                    ZipFile.CreateFromDirectory(projectPath, tempPath);
                    byte[] zipData = await System.IO.File.ReadAllBytesAsync(tempPath);
                    await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"Zip created: {tempPath}, Size: {zipData.Length} bytes");

                    await _hubContext.Clients.Client(agent.ConnectionId).SendAsync("SendZipFile", request.AgentId, processId, zipData);
                    await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"Zip sent: AgentId: {request.AgentId}, ConnectionId: {agent.ConnectionId}, ProcessId: {processId}");

                    await _hubContext.Clients.Client(agent.ConnectionId).SendAsync("ExecuteAllScenarios", processId, project.LanguageType);
                    await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"ExecuteAllScenarios called: AgentId: {request.AgentId}, ProcessId: {processId}, LanguageType: {project.LanguageType}");

                    System.IO.File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"FAILED: Zip creation/sending error: {ex.Message}");
                    return Json(new { success = false, message = $"Zip creation/sending error: {ex.Message}" });
                }
            }
            else
            {
                processId = _scenarioProcessor.StartAllScenarios(user.Id, project, request);
                await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"Local execution started: ProcessId: {processId}");
            }

            return Json(new { success = true, processId });
        }
        catch (Exception ex)
        {
            await _logService.LogAsync(User.Identity.Name, "RunAllScenarios", $"FAILED: {ex.Message}");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetProcessStatus(string processId)
    {
        var process = _processService.GetProcess(processId);
        if (process == null)
        {
            return Json(new { status = "NotFound" });
        }

        return Json(new
        {
            status = process.Status.ToString(),
            output = process.Output,
            htmlReport = process.HtmlReport,
            startTime = process.StartTime.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentProcesses()
    {
        try
        {
            var user = await _userService.GetUserByEmail(User.Identity.Name);
            var recentProcesses = _processService.GetRecentProcesses(user.Id, 5)
                .Select(p => new
                {
                    id = p.Id,
                    startTime = p.StartTime,
                    status = p.Status.ToString(),
                    output = p.Output,
                    htmlReport = p.HtmlReport
                });
        
            return Ok(recentProcesses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CancelProcess([FromBody] CancelProcessRequest request)
    {
        var user = await _userService.GetUserByEmail(User.Identity.Name);
        var result = _processService.CancelProcess(request.ProcessId, user.Id);
        return Json(new { success = result });
    }
}