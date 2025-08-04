using Gheetah.Helper;
using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.ProcessModel;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.ScenarioModel;
using Hangfire;
using Microsoft.AspNetCore.SignalR;

namespace Gheetah.Services.ScenarioProcessor
{
    public class JavaAllScenariosExecutor
    {
        private readonly IProcessService _processService;
        private readonly IProjectService _projectService;
        private readonly IHubContext<GheetahHub> _hubContext;

        public JavaAllScenariosExecutor(IProcessService processService, IHubContext<GheetahHub> hubContext, IProjectService projectService)
        {
            _processService = processService;
            _hubContext = hubContext;
            _projectService = projectService;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ExecuteAsync(
            string processId,
            Project project,
            RunAllScenariosRequest request,
            CancellationToken cancellationToken)
        {
            var processInfo = _processService.GetProcess(processId);
            if (processInfo == null) return;

            try
            {
                foreach (var projectInfo in project.ProjectInfos)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    var buildedTestFileFullPath = projectInfo.BuildedTestFileFullPath;
                    if (string.IsNullOrEmpty(buildedTestFileFullPath)) continue;

                    var testResultsFilePath = ScenarioHelper.GetTestResultsFilePath(
                        buildedTestFileFullPath,
                        null
                    );

                    string command;
                    if (projectInfo.BuildInfoFileName == "pom.xml")
                    {
                        command = $@"
                            cd '{projectInfo.BuildInfoFileFullPath}'
                            mvn test";
                    }
                    else if (projectInfo.BuildInfoFileName == "build.gradle")
                    {
                        command = $@"
                            cd '{projectInfo.BuildInfoFileFullPath}'
                            gradle clean test";
                    }
                    else
                    {
                        await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", "Unsupported build file.");
                        continue;
                    }
                    
                    if (!string.IsNullOrEmpty(request.AgentId))
                    {
                        await _hubContext.Clients.Group(processId).SendAsync("ReceiveOutput", $"Running on the agent: {request.AgentId}");
                    }
                    else
                    {
                        await _processService.ExecuteProcessAsync(
                            command,
                            processInfo,
                            testResultsFilePath
                        );
                    }

                    var reportPath = Path.Combine(projectInfo.BuildInfoFileFullPath, "target", "cucumber-reports.html");
                    if (File.Exists(reportPath))
                    {
                        processInfo.HtmlReport = await File.ReadAllTextAsync(reportPath);
                        await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveHtmlReport", processInfo.HtmlReport);
                    }
                    else
                    {
                        await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", "HTML report not found.");
                    }
                }

                processInfo.Status = ProcessStatus.Executed;
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveCompletionMessage", "The scenario ran successfully");
            }
            catch (Exception ex)
            {
                processInfo.Status = ProcessStatus.Failed;
                processInfo.Output.Add($"Error: {ex.Message}");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", $"Error: {ex.Message}");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveCompletionMessage", $"Scenario execution failed: {ex.Message}");
                throw;
            }
        }
    }
}
