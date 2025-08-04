using Gheetah.Interfaces;
using Gheetah.Models.CICDModel;
using Gheetah.Models.Hangfire;
using Gheetah.Models.ViewModels.Dashboard;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gheetah.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IAzureDevopsService _azureDevopsService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            IAzureDevopsService azureDevopsService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _azureDevopsService = azureDevopsService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var vm = await _dashboardService.GetDashboardData();
            ViewBag.CICDSettings = await _dashboardService.GetCICDSettings();
    
            ViewBag.CICDToolTypes = new Dictionary<int, string>
            {
                { (int)CICDToolType.None, "None" },
                { (int)CICDToolType.GitLab, "GitLab" },
                { (int)CICDToolType.Jenkins, "Jenkins" },
                { (int)CICDToolType.Azure, "Azure" }
            };
    
            return View(vm);
        }

        public IActionResult Guide()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CheckResults(string cicdToolId, int runId)
        {
            try
            {
                var setting = (await _dashboardService.GetCICDSettings()).FirstOrDefault(s => s.Id == cicdToolId);
                if (setting == null) return NotFound("CI/CD setting not found");

                switch (setting.ToolType)
                {
                    case CICDToolType.Azure:
                        var testResults = await _azureDevopsService.GetFailedTestResultsAsync(setting.AccessToken, setting.ApiUrl, setting.Project, runId);

                        return View(testResults);
            
                    case CICDToolType.Jenkins:
                        // Implement Jenkins pipeline data retrieval
                        // return Ok(await _jenkinsService.GetPipelineRunsAsync(project, pipeline));
                        return Ok(new List<object>());
                
                    case CICDToolType.GitLab:
                        // Implement GitLab pipeline data retrieval
                        // return Ok(await _gitlabService.GetPipelineRunsAsync(project, pipeline));
                        return Ok(new List<object>());
                
                    default:
                        return BadRequest("Unsupported CI/CD tool type");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test results");
                return StatusCode(500, ex.Message);
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDashboardWidgets([FromBody] DashboardVm model)
        {
            try
            {
                if (model?.DashboardWidgets == null)
                {
                    _logger.LogWarning("Attempted to save null widgets");
                    return BadRequest("Widget data cannot be null");
                }

                _logger.LogInformation($"Saving {model.DashboardWidgets.Count} widgets");
                await _dashboardService.SaveDashboardWidgets(model.DashboardWidgets);
        
                var updatedData = await _dashboardService.GetDashboardData();
                return Ok(new { 
                    success = true, 
                    widgets = updatedData.DashboardWidgets 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving widgets");
                return StatusCode(500, new { 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var vm = await _dashboardService.GetDashboardData();
            return Ok(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetAzureProjects(string apiUrl, string accessToken)
        {
            try
            {
                var projects = await _azureDevopsService.GetProjectsAsync(apiUrl, accessToken);
                return Ok(projects.Select(p => new { p.Id, p.Name }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Azure projects");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAzurePipelines(string apiUrl, string project, string accessToken)
        {
            try
            {
                var pipelines = await _azureDevopsService.GetPipelinesAsync(apiUrl, project, accessToken);
                return Ok(pipelines.Select(p => new { p.Id, p.Name }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Azure pipelines");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPipelineData(string cicdToolId, string project, string pipeline)
        {
            try
            {
                if (!int.TryParse(pipeline, out int pipelineId))
                {
                    return BadRequest("Invalid pipeline ID");
                }

                var setting = (await _dashboardService.GetCICDSettings()).FirstOrDefault(s => s.Id == cicdToolId);
                if (setting == null) return NotFound("CI/CD setting not found");

                switch ((CICDToolType)setting.ToolType)
                {
                    case CICDToolType.Azure:
                        var runs = await _azureDevopsService.GetPipelineRunsAsync(
                            setting.AccessToken, 
                            setting.ApiUrl, 
                            project, 
                            pipelineId);

                        return Ok(runs.OrderByDescending(r => r.FinishTime)
                            .Take(5)
                            .Select(r => new {
                                name = r.BuildNumber,
                                result = r.Result,
                                duration = (int)(r.FinishTime - r.StartTime).TotalMinutes,
                                finishedDate = r.FinishTime,
                                runId = r.Id
                            }));
            
                    case CICDToolType.Jenkins:
                        // Implement Jenkins pipeline data retrieval
                        // return Ok(await _jenkinsService.GetPipelineRunsAsync(project, pipeline));
                        return Ok(new List<object>());
                
                    case CICDToolType.GitLab:
                        // Implement GitLab pipeline data retrieval
                        // return Ok(await _gitlabService.GetPipelineRunsAsync(project, pipeline));
                        return Ok(new List<object>());
                
                    default:
                        return BadRequest("Unsupported CI/CD tool type");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pipeline data");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTestResultsData(string cicdToolId, string project, string pipeline)
        {
            try
            { 
                if (!int.TryParse(pipeline, out int pipelineId))
                {
                    return BadRequest("Invalid pipeline ID");
                }
                var setting = (await _dashboardService.GetCICDSettings()).FirstOrDefault(s => s.Id == cicdToolId);
                if (setting == null) return NotFound();

                switch ((CICDToolType)setting.ToolType)
                {
                    case CICDToolType.Azure:
                        var testRuns = await _azureDevopsService.GetTestRunsForPipelineAsync(setting.AccessToken,setting.ApiUrl, setting.Organization, project, pipelineId);

                        return Ok(testRuns);
                    
                    case CICDToolType.Jenkins:
                        // Implement Jenkins test results retrieval
                        // return Ok(await _jenkinsService.GetTestResultsAsync(project, pipelineName));
                        return Ok(new List<object>());
                        
                    case CICDToolType.GitLab:
                        // Implement GitLab test results retrieval
                        // return Ok(await _gitlabService.GetTestResultsAsync(project, pipelineName));
                        return Ok(new List<object>());
                        
                    default:
                        return BadRequest("Unsupported CI/CD tool type");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test results data");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult GetRecentJobs(int count = 10)
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var allJobs = new List<JobInfoDto>();

                var succeededJobs = monitoringApi.SucceededJobs(0, count);
                foreach (var job in succeededJobs)
                {
                    allJobs.Add(new JobInfoDto
                    {
                        Id = job.Key,
                        State = "Succeeded",
                        CreatedAt = job.Value.SucceededAt?.ToLocalTime() ?? DateTime.MinValue,
                        MethodName = job.Value.Job?.Method?.Name ?? "Unknown",
                        Arguments = JsonConvert.SerializeObject(job.Value.Job?.Args) ?? string.Empty
                    });
                }

                var failedJobs = monitoringApi.FailedJobs(0, count);
                foreach (var job in failedJobs)
                {
                    allJobs.Add(new JobInfoDto
                    {
                        Id = job.Key,
                        State = "Failed",
                        CreatedAt = job.Value.FailedAt?.ToLocalTime() ?? DateTime.MinValue,
                        MethodName = job.Value.Job?.Method?.Name ?? "Unknown",
                        Arguments = JsonConvert.SerializeObject(job.Value.Job?.Args) ?? string.Empty
                    });
                }

                var processingJobs = monitoringApi.ProcessingJobs(0, count);
                foreach (var job in processingJobs)
                {
                    allJobs.Add(new JobInfoDto
                    {
                        Id = job.Key,
                        State = "Processing",
                        CreatedAt = job.Value.StartedAt.HasValue 
                            ? job.Value.StartedAt.Value.ToLocalTime() 
                            : DateTime.MinValue,
                        MethodName = job.Value.Job?.Method?.Name ?? "Unknown",
                        Arguments = JsonConvert.SerializeObject(job.Value.Job?.Args) ?? string.Empty
                    });
                }

                var result = allJobs
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(count)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent jobs");
                return StatusCode(500, new { 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public IActionResult GetJobDetails(string jobId)
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var jobDetails = monitoringApi.JobDetails(jobId);

                var job = jobDetails.Job;

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        Job = new
                        {
                            Type = job?.Method?.DeclaringType?.FullName ?? "N/A",
                            Method = job?.Method?.Name ?? "N/A",
                            CreatedAt = jobDetails.History.FirstOrDefault()?.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss") ?? "N/A"
                        },

                        State = jobDetails.History.LastOrDefault()?.StateName ?? "N/A",
                        Reason = !string.IsNullOrEmpty(jobDetails.History.LastOrDefault()?.Reason) 
                            ? jobDetails.History.LastOrDefault()?.Reason 
                            : "No reason provided",

                        History = jobDetails.History.Select(h => new
                        {
                            State = h.StateName ?? "N/A",
                            Reason = !string.IsNullOrEmpty(h.Reason) ? h.Reason : "N/A",
                            Date = h.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss")
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

    }
}