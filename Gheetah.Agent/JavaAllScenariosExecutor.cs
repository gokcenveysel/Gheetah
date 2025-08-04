using System.Diagnostics;

namespace Gheetah.Agent
{
    public static class JavaAllScenariosExecutor
    {
        public static async Task ExecuteAllAsync(string projectPath,string processId)
        {
            bool xmlReportGenerated = false;
            string zipFilePath = projectPath + ".zip";
            try
            {
                StatusUI.ShowStatus($"Entering JavaAllScenariosExecutor.ExecuteAsync: ProjectPath={projectPath}, ProcessId={processId}");
                LogToFile($"Entering JavaAllScenariosExecutor.ExecuteAsync: ProjectPath={projectPath} ProcessId={processId}");

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
                    command = $@"cd '{buildDir}'; mvn test";
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
                            StatusUI.ShowStatus($"Error: {e.Data}");
                            LogToFile($"Error: {e.Data}");
                            await SendOutputWithTimeout($"Error: {e.Data}", processId);
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
                        return;
                    }
                }

                string testResultsDir = Path.Combine(buildDir, "TestResults");
                string testResultsFilePath = null;
                try
                {
                    if (Directory.Exists(testResultsDir))
                    {
                        var xmlFiles = Directory.GetFiles(testResultsDir, $"*_test_results.xml")
                            .OrderByDescending(f => File.GetLastWriteTime(f))
                            .ToList();
                        if (xmlFiles.Any())
                        {
                            testResultsFilePath = xmlFiles.First().Replace("\\", "/");
                            StatusUI.ShowStatus($"Found XML report: {testResultsFilePath}");
                            LogToFile($"Found XML report: {testResultsFilePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Error searching for XML report: {ex.Message}, StackTrace: {ex.StackTrace}");
                    LogToFile($"Error searching for XML report: {ex.Message}, StackTrace: {ex.StackTrace}");
                    await SendOutputWithTimeout($"Error searching for XML report: {ex.Message}", processId);
                    return;
                }

                if (!string.IsNullOrEmpty(testResultsFilePath) && File.Exists(testResultsFilePath))
                {
                    StatusUI.ShowStatus($"Reading XML report: {testResultsFilePath}");
                    LogToFile($"Reading XML report: {testResultsFilePath}");
                    string xmlReport;
                    try
                    {
                        xmlReport = await File.ReadAllTextAsync(testResultsFilePath);
                    }
                    catch (Exception ex)
                    {
                        StatusUI.ShowStatus($"Error reading XML report: {ex.Message}, StackTrace: {ex.StackTrace}");
                        LogToFile($"Error reading XML report: {ex.Message}, StackTrace: {ex.StackTrace}");
                        await SendOutputWithTimeout($"Error reading XML report: {ex.Message}", processId);
                        return;
                    }
                    StatusUI.ShowStatus($"Sending XML report: {testResultsFilePath}");
                    LogToFile($"Sending XML report: {testResultsFilePath}");
                    var sendResultTask = AgentService.SendResultAsync($"TestResult:{xmlReport}", processId);
                    if (await Task.WhenAny(sendResultTask, Task.Delay(30000)) == sendResultTask)
                    {
                        await sendResultTask;
                        StatusUI.ShowStatus($"Successfully sent XML report: {testResultsFilePath}");
                        LogToFile($"Successfully sent XML report: {testResultsFilePath}");
                    }
                    else
                    {
                        StatusUI.ShowStatus($"Timeout sending XML report: {testResultsFilePath}");
                        LogToFile($"Timeout sending XML report: {testResultsFilePath}");
                        await SendOutputWithTimeout($"Timeout sending XML report: {testResultsFilePath}", processId);
                        return;
                    }
                    await SendOutputWithTimeout($"XML report generated: {testResultsFilePath}", processId);
                    xmlReportGenerated = true;
                }
                else
                {
                    StatusUI.ShowStatus($"Error: XML report not found in {testResultsDir}");
                    LogToFile($"Error: XML report not found in {testResultsDir}");
                    await SendOutputWithTimeout($"Error: XML report not found in {testResultsDir}", processId);
                }
            }
            catch (Exception ex)
            {
                StatusUI.ShowStatus($"Error executing Java scenario: {ex.Message}, StackTrace: {ex.StackTrace}");
                LogToFile($"Error executing Java scenario: {ex.Message}, StackTrace: {ex.StackTrace}");
                await SendOutputWithTimeout($"Error executing Java scenario: {ex.Message}", processId);
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

        private static void LogToFile(string message)
        {
            try
            {
                string logPath = Path.Combine(Path.GetTempPath(), "Gheetah_Agent.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] JavaScenarioExecutor: {message}{Environment.NewLine}");
            }
            catch
            {
                // Silent fail for logging
            }
        }
    }
}