using Gheetah.Helper;
using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.ProcessModel;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.ScenarioModel;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Gheetah.Services.ScenarioProcessor
{
    public class JavaAllScenariosExecutor
    {
        private readonly IProcessService _processService;
        private readonly IProjectService _projectService;
        private readonly ITestResultProcessor _testResultProcessor;
        private readonly IHubContext<GheetahHub> _hubContext;

        public JavaAllScenariosExecutor(IProcessService processService, IHubContext<GheetahHub> hubContext, IProjectService projectService, ITestResultProcessor testResultProcessor)
        {
            _processService = processService;
            _hubContext = hubContext;
            _projectService = projectService;
            _testResultProcessor = testResultProcessor;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ExecuteAllAsync(
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

                    var buildedTestFileFullPath = projectInfo.BuildInfoFileFullPath;
                    if (string.IsNullOrEmpty(buildedTestFileFullPath)) continue;

                    string command;
                    if (projectInfo.BuildInfoFileName == "pom.xml")
                    {
                        command = $@"
                            cd '{projectInfo.BuildInfoFileFullPath}'
                            mvn clean test";
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

                    Console.WriteLine($"Executing all scenarios for processId: {processId}, agentId: {request.AgentId}");

                    if (!string.IsNullOrEmpty(request.AgentId))
                    {
                        await _hubContext.Clients.Group(processId).SendAsync("ReceiveOutput", $"Running on the agent: {request.AgentId}");
                    }
                    else
                    {
                        await _processService.ExecuteProcessAsync(
                            command,
                            processInfo,
                            null
                        );

                        var testResultsFileName = ExtractTestResultsFilePath(processInfo.Output, "AllScenarios");
                        if (string.IsNullOrEmpty(testResultsFileName))
                        {
                            await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", "Error: Test results file not found for all scenarios");
                            continue;
                        }

                        var relativeTestResultFilePath = Path.Combine(projectInfo.BuildInfoFileFullPath, "TestResults", testResultsFileName);
                        if (!File.Exists(relativeTestResultFilePath))
                        {
                            await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", $"Error: Test results file not found at {relativeTestResultFilePath}");
                            continue;
                        }

                        await _testResultProcessor.ProcessTestResultsAsync(processInfo, relativeTestResultFilePath);
                    }
                }

                processInfo.Status = ProcessStatus.Executed;
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveCompletionMessage", "All scenarios executed successfully");
            }
            catch (Exception ex)
            {
                processInfo.Status = ProcessStatus.Failed;
                processInfo.Output.Add($"Error: {ex.Message}");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", $"Error: {ex.Message}");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveCompletionMessage", $"All scenarios execution failed: {ex.Message}");
                throw;
            }
        }

        private string ExtractTestResultsFilePath(List<string> outputLines, string scenarioTag)
        {
            var pattern = @"XML report written successfully to: TestResults/(.*?)\.xml";
            foreach (var line in outputLines)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    var fileName = match.Groups[1].Value + ".xml";
                    return fileName;
                }
            }
            return null;
        }
    }
}