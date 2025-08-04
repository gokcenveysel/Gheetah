using System.Diagnostics;

namespace Gheetah.Agent
{
    public static class CSharpAllScenariosExecutor
    {
        public static async Task ExecuteAllAsync(string projectPath,string processId, string buildedTestFileName)
        {
            StatusUI.ShowStatus($"Executing C# scenario: ProjectPath={projectPath},ProcessId={processId}, BuildedTestFileName={buildedTestFileName}");
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

                string testResultsFilePath = AgentService.GetTestResultsFilePath(dllDir, scenarioTag:null);
                StatusUI.ShowStatus($"Test results file path: {testResultsFilePath}");

                string powerShellCommand = $@"cd '{dllDir}'; dotnet test -v detailed '{buildedTestFileName}' --logger 'trx;LogFileName={testResultsFilePath}' --no-build --no-restore";
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
                            StatusUI.ShowStatus($"Error: {e.Data}");
                            await AgentService.SendOutputAsync($"Error: {e.Data}", processId);
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
    }
}