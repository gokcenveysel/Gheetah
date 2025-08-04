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
    public class CSharpScenarioExecutor
        {
            private readonly IProcessService _processService;
            private readonly IProjectService _projectService;
            private readonly IHubContext<GheetahHub> _hubContext;

            public CSharpScenarioExecutor(IProcessService processService, IHubContext<GheetahHub> hubContext, IProjectService projectService)
            {
                _processService = processService;
                _hubContext = hubContext;
                _projectService = projectService;
            }

            [AutomaticRetry(Attempts = 0)]
            public async Task ExecuteAsync(
                string processId, 
                Project project, 
                RunScenarioRequest request, 
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

                        var testResultsFilePath = ScenarioHelper.GetTestResultsFilePath(buildedTestFileFullPath, request.ScenarioTag);

                        var powerShellCommand = $@"
                            cd '{buildedTestFileFullPath}'
                            dotnet test -v detailed '{projectInfo.BuildedTestFileName}' --filter 'Category={request.ScenarioTag}' --logger 'trx;LogFileName={testResultsFilePath}'";

                        Console.WriteLine($"Executing scenario for processId: {processId}, agentId: {request.AgentId}");

                        if (!string.IsNullOrEmpty(request.AgentId))
                        {
                            Console.WriteLine($"Running on agent: {request.AgentId}");
                            await _hubContext.Clients.Group(processId).SendAsync("ReceiveOutput", $"Running on agent: {request.AgentId}");
                        }
                        else
                        {
                            Console.WriteLine("Running locally");
                            await _processService.ExecuteProcessAsync(
                                powerShellCommand,
                                processInfo,
                                testResultsFilePath
                            );
                        }
                    }
                    await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveHtmlReport", processInfo.HtmlReport);
                    processInfo.Status = ProcessStatus.Executed;
                    await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveCompletionMessage", "Scenario executed successfully");
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
