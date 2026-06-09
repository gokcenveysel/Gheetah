using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.ProcessModel;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.ScenarioModel;
using Hangfire;
using Microsoft.AspNetCore.SignalR;

namespace Gheetah.Services.ScenarioProcessor
{
    public class PlaywrightAllScenariosExecutor
    {
        private readonly IProcessService _processService;
        private readonly IHubContext<GheetahHub> _hubContext;

        public PlaywrightAllScenariosExecutor(IProcessService processService, IHubContext<GheetahHub> hubContext)
        {
            _processService = processService;
            _hubContext = hubContext;
        }

        private static string BuildGheetahPlaywrightConfig(string testDir = "tests") =>
$@"// @ts-nocheck
import {{ defineConfig, devices }} from '@playwright/test';
export default defineConfig({{
  testDir: './{testDir.Replace("\\", "/")}',
  timeout: 60000,
  retries: 0,
  workers: 1,
  reporter: [['json', {{ outputFile: 'test-results/results.json' }}]],
  use: {{
    headless: true,
    screenshot: 'on',
    video: 'on',
    trace: 'off',
  }},
  projects: [{{ name: 'chromium', use: {{ ...devices['Desktop Chrome'] }} }}],
}});
";

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

                    var projectDir = projectInfo.BuildInfoFileFullPath;
                    if (string.IsNullOrEmpty(projectDir)) continue;

                    var resultsFilePath = Path.Combine(projectDir, "test-results", "results.json");
                    var tempConfigPath = Path.Combine(projectDir, "gheetah-runner.config.ts");

                    if (!string.IsNullOrEmpty(request.AgentId))
                    {
                        await _hubContext.Clients.Group(processId).SendAsync("ReceiveOutput", $"Running on agent: {request.AgentId}");
                    }
                    else
                    {
                        try
                        {
                            var testDir = "tests";
                            if (!string.IsNullOrEmpty(projectInfo.FeatureFilesPath) && Directory.Exists(projectInfo.FeatureFilesPath))
                            {
                                var rel = Path.GetRelativePath(projectDir, projectInfo.FeatureFilesPath);
                                if (!string.IsNullOrEmpty(rel) && rel != ".") testDir = rel.Replace("\\", "/");
                            }
                            await File.WriteAllTextAsync(tempConfigPath, BuildGheetahPlaywrightConfig(testDir));
                            var command = $@"
                                cd '{projectDir}'
                                npx playwright test --config gheetah-runner.config.ts";
                            await _processService.ExecuteProcessAsync(command, processInfo, resultsFilePath);
                        }
                        finally
                        {
                            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
                        }
                    }
                }

                processInfo.Status = ProcessStatus.Executed;
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveCompletionMessage", "All Playwright tests executed successfully");
            }
            catch (Exception ex)
            {
                processInfo.Status = ProcessStatus.Failed;
                processInfo.Output.Add($"Error: {ex.Message}");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", $"Error: {ex.Message}");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveCompletionMessage", $"Playwright test execution failed: {ex.Message}");
                throw;
            }
        }
    }
}
