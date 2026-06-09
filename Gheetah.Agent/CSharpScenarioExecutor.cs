using System.Diagnostics;

namespace Gheetah.Agent
{
    public static class CSharpScenarioExecutor
    {
        public static async Task ExecuteAsync(string projectPath, string scenarioTag, string processId, string buildedTestFileName)
        {
            StatusUI.ShowStatus($"Executing C# scenario: ProjectPath={projectPath}, ScenarioTag={scenarioTag}, ProcessId={processId}, BuildedTestFileName={buildedTestFileName}");
            try
            {
                StatusUI.ShowStatus($"Searching for {buildedTestFileName} in {projectPath}");
                string[] dllFiles = Directory.GetFiles(projectPath, buildedTestFileName, SearchOption.AllDirectories);
                if (dllFiles.Length == 0)
                {
                    StatusUI.ShowStatus($"Error: {buildedTestFileName} not found in {projectPath}");
                    await AgentService.SendOutputAsync($"Error: {buildedTestFileName} not found in {projectPath}", processId);
                    return;
                }

                StatusUI.ShowStatus($"Found {dllFiles.Length} {buildedTestFileName} files: {string.Join(", ", dllFiles)}");
                string dllPath = dllFiles[0];
                string dllDir = Path.GetDirectoryName(dllPath);
                StatusUI.ShowStatus($"Selected .dll file: {dllPath}, Directory: {dllDir}");

                string testResultsFilePath = AgentService.GetTestResultsFilePath(dllDir, scenarioTag: scenarioTag);
                StatusUI.ShowStatus($"Test results file path: {testResultsFilePath}");

                string powerShellCommand = $@"cd '{dllDir}'; dotnet test '{buildedTestFileName}' --filter 'Category={scenarioTag}' --logger 'trx;LogFileName={testResultsFilePath}'";
                StatusUI.ShowStatus($"Running command: {powerShellCommand}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"{powerShellCommand}\"",
                    WorkingDirectory = dllDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += async (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            StatusUI.ShowStatus($"Output: {e.Data}");
                            await AgentService.SendOutputAsync(e.Data, processId);
                        }
                    };
                    process.ErrorDataReceived += async (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            StatusUI.ShowStatus($"Stderr: {e.Data}");
                            var msg = IsNonErrorStderr(e.Data) ? e.Data : $"Error: {e.Data}";
                            await AgentService.SendOutputAsync(msg, processId);
                        }
                    };

                    StatusUI.ShowStatus($"Starting process: {powerShellCommand}");
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    StatusUI.ShowStatus($"Process exited with code: {process.ExitCode}");
                    if (process.ExitCode == 0)
                    {
                        await AgentService.SendOutputAsync("Test execution completed successfully.", processId);
                    }
                    else
                    {
                        await AgentService.SendOutputAsync($"Test execution failed with exit code {process.ExitCode}.", processId);
                    }
                }

                if (File.Exists(testResultsFilePath))
                {
                    string trxContent = await File.ReadAllTextAsync(testResultsFilePath);
                    StatusUI.ShowStatus($"Sending .trx file: {testResultsFilePath}");
                    await AgentService.SendResultAsync($"TestResult:{trxContent}", processId);
                    await AgentService.SendOutputAsync($"TRX file generated: {testResultsFilePath}", processId);
                }
                else
                {
                    StatusUI.ShowStatus($"Error: TRX file not found at {testResultsFilePath}");
                    await AgentService.SendOutputAsync($"Error: TRX file not found at {testResultsFilePath}", processId);
                }
            }
            catch (Exception ex)
            {
                StatusUI.ShowStatus($"Error executing C# scenario: {ex.Message}, StackTrace: {ex.StackTrace}");
                await AgentService.SendOutputAsync($"Error executing C# scenario: {ex.Message}", processId);
                await AgentService.SendResultAsync($"Error:Scenario execution failed:{ex.Message}", processId);
            }
            finally
            {
                try
                {
                    Directory.Delete(projectPath, true);
                    StatusUI.ShowStatus($"Cleaned up project directory: {projectPath}");
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Cleanup error: {ex.Message}");
                }
            }
        }
        private static bool IsNonErrorStderr(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\[.+\]\s+(INFO|WARN|WARNING)\s+")) return true;
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\w{3}\s+\d{1,2},\s+\d{4}\s+\d{1,2}:\d{2}:\d{2}\s+(AM|PM)")) return true;
            if (line.TrimStart().StartsWith("WARNING:") && !line.Contains("FAILURE") && !line.Contains("ERROR")) return true;
            return false;
        }
    }
}