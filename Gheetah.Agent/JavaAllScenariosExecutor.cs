using System.Diagnostics;
using System.Linq;

namespace Gheetah.Agent
{
    public static class JavaAllScenariosExecutor
    {
        public static async Task ExecuteAllAsync(string projectPath, string processId)
        {
            bool xmlReportGenerated = false;
            string zipFilePath = projectPath + ".zip";
            try
            {
                StatusUI.ShowStatus($"Entering JavaAllScenariosExecutor.ExecuteAsync: ProjectPath={projectPath}, ProcessId={processId}");
                LogToFile($"Entering JavaAllScenariosExecutor.ExecuteAsync: ProjectPath={projectPath}, ProcessId={processId}");

                StatusUI.ShowStatus($"Checking project directory: {projectPath}");
                LogToFile($"Checking project directory: {projectPath}");
                if (!Directory.Exists(projectPath))
                {
                    StatusUI.ShowStatus($"Error: Project directory not found: {projectPath}");
                    LogToFile($"Error: Project directory not found: {projectPath}");
                    await SendOutputWithTimeout($"Error: Project directory not found: {projectPath}", processId);
                    return;
                }

                StatusUI.ShowStatus($"Searching for pom.xml or build.gradle in {projectPath} and subdirectories");
                LogToFile($"Searching for pom.xml or build.gradle in {projectPath} and subdirectories");
                string[] pomFiles;
                string[] gradleFiles;
                string[] testngFiles;
                try
                {
                    pomFiles = Directory.GetFiles(projectPath, "pom.xml", SearchOption.AllDirectories);
                    gradleFiles = Directory.GetFiles(projectPath, "build.gradle", SearchOption.AllDirectories);
                    testngFiles = Directory.GetFiles(projectPath, "testng.xml", SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Error searching for build files: {ex.Message}, StackTrace: {ex.StackTrace}");
                    LogToFile($"Error searching for build files: {ex.Message}, StackTrace: {ex.StackTrace}");
                    await SendOutputWithTimeout($"Error searching for build files: {ex.Message}", processId);
                    return;
                }

                string buildFilePath = null;
                string buildFileName = null;
                string testngFilePath = testngFiles.Length > 0 ? testngFiles[0] : null;

                if (pomFiles.Length > 0)
                {
                    buildFilePath = pomFiles[0];
                    buildFileName = "pom.xml";
                    StatusUI.ShowStatus($"Found pom.xml file: {buildFilePath}");
                    LogToFile($"Found pom.xml file: {buildFilePath}");
                }
                else if (gradleFiles.Length > 0)
                {
                    buildFilePath = gradleFiles[0];
                    buildFileName = "build.gradle";
                    StatusUI.ShowStatus($"Found build.gradle file: {buildFilePath}");
                    LogToFile($"Found build.gradle file: {buildFilePath}");
                }
                else
                {
                    StatusUI.ShowStatus($"Error: No pom.xml or build.gradle file found in {projectPath} or subdirectories");
                    LogToFile($"Error: No pom.xml or build.gradle file found in {projectPath} or subdirectories");
                    await SendOutputWithTimeout($"Error: No pom.xml or build.gradle file found in {projectPath} or subdirectories", processId);
                    return;
                }

                if (!string.IsNullOrEmpty(testngFilePath))
                {
                    StatusUI.ShowStatus($"Found testng.xml file: {testngFilePath}");
                    LogToFile($"Found testng.xml file: {testngFilePath}");
                }

                string buildDir = Path.GetDirectoryName(buildFilePath);
                StatusUI.ShowStatus($"Using build directory as base: {buildDir}");
                LogToFile($"Using build directory as base: {buildDir}");

                if (!Directory.Exists(buildDir))
                {
                    StatusUI.ShowStatus($"Error: Build directory not found: {buildDir}");
                    LogToFile($"Error: Build directory not found: {buildDir}");
                    await SendOutputWithTimeout($"Error: Build directory not found: {buildDir}", processId);
                    return;
                }

                string command;
                if (pomFiles.Length > 0)
                {
                    command = $@"cd '{buildDir}'; mvn clean test";
                }
                else if (gradleFiles.Length > 0)
                {
                    command = $@"cd '{buildDir}'; gradle clean test";
                }
                else
                {
                    StatusUI.ShowStatus($"Error: No pom.xml or build.gradle file found in {projectPath} or subdirectories");
                    LogToFile($"Error: No pom.xml or build.gradle file found in {projectPath} or subdirectories");
                    await SendOutputWithTimeout($"Error: No pom.xml or build.gradle file found in {projectPath} or subdirectories", processId);
                    return;
                }
                StatusUI.ShowStatus($"Executing command in {buildDir}: {command}");
                LogToFile($"Executing command in {buildDir}: {command}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"{command}\"",
                    WorkingDirectory = buildDir,
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
                            LogToFile($"Output: {e.Data}");
                            await SendOutputWithTimeout($"Output: {e.Data}", processId);
                        }
                    };
                    process.ErrorDataReceived += async (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            StatusUI.ShowStatus($"Stderr: {e.Data}");
                            LogToFile($"Stderr: {e.Data}");
                            var msg = IsNonErrorStderr(e.Data) ? e.Data : $"Error: {e.Data}";
                            await SendOutputWithTimeout(msg, processId);
                        }
                    };

                    StatusUI.ShowStatus($"Starting process in {buildDir}: {startInfo.FileName} {startInfo.Arguments}");
                    LogToFile($"Starting process in {buildDir}: {startInfo.FileName} {startInfo.Arguments}");
                    try
                    {
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        await process.WaitForExitAsync();
                    }
                    catch (Exception ex)
                    {
                        StatusUI.ShowStatus($"Error starting process: {ex.Message}, StackTrace: {ex.StackTrace}");
                        LogToFile($"Error starting process: {ex.Message}, StackTrace: {ex.StackTrace}");
                        await SendOutputWithTimeout($"Error starting process: {ex.Message}", processId);
                        throw;
                    }

                    StatusUI.ShowStatus($"Process exited with code: {process.ExitCode}");
                    LogToFile($"Process exited with code: {process.ExitCode}");
                    await SendOutputWithTimeout($"Process exited with code: {process.ExitCode}", processId);
                    if (process.ExitCode == 0)
                    {
                        await SendOutputWithTimeout("Test execution completed successfully.", processId);
                    }
                    else
                    {
                        await SendOutputWithTimeout($"Test execution failed with exit code {process.ExitCode}.", processId);
                    }
                }

                // Look for Cucumber JSON report in target/cucumber-reports/
                string cucumberReportsDir = Path.Combine(buildDir, "target", "cucumber-reports");
                string cucumberJsonPath = null;
                try
                {
                    if (Directory.Exists(cucumberReportsDir))
                    {
                        var jsonFiles = Directory.GetFiles(cucumberReportsDir, "*.json", SearchOption.AllDirectories)
                            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                            .ToArray();
                        if (jsonFiles.Length > 0)
                        {
                            cucumberJsonPath = jsonFiles[0];
                            StatusUI.ShowStatus($"Found Cucumber JSON report: {cucumberJsonPath}");
                            LogToFile($"Found Cucumber JSON report: {cucumberJsonPath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Error searching for Cucumber JSON report: {ex.Message}");
                    LogToFile($"Error searching for Cucumber JSON report: {ex.Message}");
                    await SendOutputWithTimeout($"Error searching for Cucumber JSON report: {ex.Message}", processId);
                    return;
                }

                if (!string.IsNullOrEmpty(cucumberJsonPath) && File.Exists(cucumberJsonPath))
                {
                    StatusUI.ShowStatus($"Sending Cucumber JSON report: {cucumberJsonPath}");
                    LogToFile($"Sending Cucumber JSON report: {cucumberJsonPath}");
                    string jsonContent;
                    try
                    {
                        jsonContent = await File.ReadAllTextAsync(cucumberJsonPath);
                    }
                    catch (Exception ex)
                    {
                        StatusUI.ShowStatus($"Error reading Cucumber JSON report: {ex.Message}");
                        LogToFile($"Error reading Cucumber JSON report: {ex.Message}");
                        await SendOutputWithTimeout($"Error reading Cucumber JSON report: {ex.Message}", processId);
                        return;
                    }
                    var sendTask = AgentService.SendResultAsync($"TestResult:{jsonContent}", processId);
                    if (await Task.WhenAny(sendTask, Task.Delay(30000)) == sendTask)
                    {
                        await sendTask;
                        StatusUI.ShowStatus($"Successfully sent Cucumber JSON report");
                        LogToFile($"Successfully sent Cucumber JSON report");
                    }
                    else
                    {
                        StatusUI.ShowStatus($"Timeout sending Cucumber JSON report");
                        LogToFile($"Timeout sending Cucumber JSON report");
                        await SendOutputWithTimeout("Timeout sending Cucumber JSON report", processId);
                        return;
                    }
                    await SendOutputWithTimeout($"Cucumber JSON report generated: {cucumberJsonPath}", processId);
                    xmlReportGenerated = true;
                }
                else
                {
                    StatusUI.ShowStatus($"Cucumber JSON report not found in {cucumberReportsDir}");
                    LogToFile($"Cucumber JSON report not found in {cucumberReportsDir}");
                    await SendOutputWithTimeout("Warning: Cucumber JSON report not found in target/cucumber-reports/", processId);
                }
            }
            catch (Exception ex)
            {
                StatusUI.ShowStatus($"Error executing Java scenarios: {ex.Message}, StackTrace: {ex.StackTrace}");
                LogToFile($"Error executing Java scenarios: {ex.Message}, StackTrace: {ex.StackTrace}");
                await SendOutputWithTimeout($"Error executing Java scenarios: {ex.Message}", processId);
                await SendResultWithTimeout($"Error:Scenario execution failed:{ex.Message}", processId);
            }
            finally
            {
                StatusUI.ShowStatus($"Entering finally block, xmlReportGenerated={xmlReportGenerated}");
                LogToFile($"Entering finally block, xmlReportGenerated={xmlReportGenerated}");
                if (xmlReportGenerated)
                {
                    try
                    {
                        StatusUI.ShowStatus($"Starting cleanup for project directory: {projectPath}");
                        LogToFile($"Starting cleanup for project directory: {projectPath}");
                        if (Directory.Exists(projectPath))
                        {
                            Directory.Delete(projectPath, true);
                            StatusUI.ShowStatus($"Cleaned up project directory: {projectPath}");
                            LogToFile($"Cleaned up project directory: {projectPath}");
                            await SendOutputWithTimeout($"Cleaned up project directory: {projectPath}", processId);
                        }
                        if (File.Exists(zipFilePath))
                        {
                            StatusUI.ShowStatus($"Starting cleanup for zip file: {zipFilePath}");
                            LogToFile($"Starting cleanup for zip file: {zipFilePath}");
                            File.Delete(zipFilePath);
                            StatusUI.ShowStatus($"Cleaned up zip file: {zipFilePath}");
                            LogToFile($"Cleaned up zip file: {zipFilePath}");
                            await SendOutputWithTimeout($"Cleaned up zip file: {zipFilePath}", processId);
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusUI.ShowStatus($"Cleanup error: {ex.Message}, StackTrace: {ex.StackTrace}");
                        LogToFile($"Cleanup error: {ex.Message}, StackTrace: {ex.StackTrace}");
                        await SendOutputWithTimeout($"Cleanup error: {ex.Message}", processId);
                    }
                }
                else
                {
                    StatusUI.ShowStatus($"Skipping cleanup due to missing XML report: {projectPath}");
                    LogToFile($"Skipping cleanup due to missing XML report: {projectPath}");
                    await SendOutputWithTimeout($"Skipping cleanup due to missing XML report: {projectPath}", processId);
                }
                StatusUI.ShowStatus($"ExecuteAsync method completed for ProcessId={processId}");
                LogToFile($"ExecuteAsync method completed for ProcessId={processId}");
            }
        }

        private static async Task SendOutputWithTimeout(string message, string processId)
        {
            var sendOutputTask = AgentService.SendOutputAsync(message, processId);
            if (await Task.WhenAny(sendOutputTask, Task.Delay(30000)) == sendOutputTask)
            {
                await sendOutputTask;
                LogToFile($"Successfully sent output: {message}");
            }
            else
            {
                StatusUI.ShowStatus($"Timeout sending output: {message}");
                LogToFile($"Timeout sending output: {message}");
            }
        }

        private static async Task SendResultWithTimeout(string message, string processId)
        {
            var sendResultTask = AgentService.SendResultAsync(message, processId);
            if (await Task.WhenAny(sendResultTask, Task.Delay(30000)) == sendResultTask)
            {
                await sendResultTask;
                LogToFile($"Successfully sent result: {message}");
            }
            else
            {
                StatusUI.ShowStatus($"Timeout sending result: {message}");
                LogToFile($"Timeout sending result: {message}");
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

        private static void LogToFile(string message)
        {
            try
            {
                string logPath = Path.Combine(Path.GetTempPath(), "Gheetah_Agent.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] JavaAllScenariosExecutor: {message}{Environment.NewLine}");
            }
            catch
            {
                // Silent fail for logging
            }
        }
    }
}