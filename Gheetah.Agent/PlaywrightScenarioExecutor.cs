using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Gheetah.Agent
{
    public static class PlaywrightScenarioExecutor
    {
        private const string GheetahConfigName = "gheetah-runner.config.ts";

        private static string BuildGheetahConfig(string testDir = "tests") =>
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

        private static string DetectTestDir(string projectDir)
        {
            foreach (var candidate in new[] { "tests", "test", "e2e", "specs", "src/tests", "src/test" })
            {
                var dir = Path.Combine(projectDir, candidate.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (Directory.Exists(dir) && Directory.GetFiles(dir, "*.spec.ts", SearchOption.AllDirectories).Any())
                    return candidate;
            }
            // Find directory of first spec file found anywhere in project
            var specFiles = Directory.GetFiles(projectDir, "*.spec.ts", SearchOption.AllDirectories);
            if (specFiles.Length > 0)
            {
                var rel = Path.GetRelativePath(projectDir, Path.GetDirectoryName(specFiles[0]));
                if (rel != ".") return rel.Replace("\\", "/");
            }
            return "tests";
        }

        public static async Task ExecuteAsync(string projectPath, string testName, string processId)
        {
            StatusUI.ShowStatus($"Executing Playwright test: ProjectPath={projectPath}, TestName={testName}, ProcessId={processId}");
            string zipFilePath = projectPath + ".zip";
            string tempConfigPath = null;
            bool resultsGenerated = false;

            try
            {
                if (!Directory.Exists(projectPath))
                {
                    await AgentService.SendOutputAsync($"Error: Project directory not found: {projectPath}", processId);
                    return;
                }

                string[] packageJsonFiles = Directory.GetFiles(projectPath, "package.json", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules")).ToArray();

                if (packageJsonFiles.Length == 0)
                {
                    await AgentService.SendOutputAsync($"Error: package.json not found in {projectPath}", processId);
                    return;
                }

                string projectDir = Path.GetDirectoryName(packageJsonFiles[0]);
                StatusUI.ShowStatus($"Using project directory: {projectDir}");

                // Write temp config with screenshot/video enabled and JSON reporter
                tempConfigPath = Path.Combine(projectDir, GheetahConfigName);
                await File.WriteAllTextAsync(tempConfigPath, BuildGheetahConfig(DetectTestDir(projectDir)));

                string resultsFilePath = Path.Combine(projectDir, "test-results", "results.json");
                string psEscapedName = testName.Replace("'", "''");
                string command = $@"cd '{projectDir}'; npx playwright test --config {GheetahConfigName} --grep '{psEscapedName}'";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"{command}\"",
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += async (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            var cleaned = StripAnsiCodes(e.Data);
                            if (string.IsNullOrWhiteSpace(cleaned)) return;
                            StatusUI.ShowStatus($"Output: {cleaned}");
                            await AgentService.SendOutputAsync(cleaned, processId);
                        }
                    };
                    process.ErrorDataReceived += async (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            var cleaned = StripAnsiCodes(e.Data);
                            if (string.IsNullOrWhiteSpace(cleaned)) return;
                            StatusUI.ShowStatus($"Stderr: {cleaned}");
                            var msg = IsNonErrorStderr(cleaned) ? cleaned : $"Error: {cleaned}";
                            await AgentService.SendOutputAsync(msg, processId);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    StatusUI.ShowStatus($"Process exited with code: {process.ExitCode}");

                    if (File.Exists(resultsFilePath))
                    {
                        string jsonContent = await File.ReadAllTextAsync(resultsFilePath);
                        StatusUI.ShowStatus($"Sending Playwright JSON results: {resultsFilePath}");
                        await AgentService.SendResultAsync($"TestResult:{jsonContent}", processId);
                        await AgentService.SendOutputAsync($"Playwright results generated: {resultsFilePath}", processId);
                        resultsGenerated = true;
                    }
                    else
                    {
                        await AgentService.SendOutputAsync($"Warning: Results file not found at {resultsFilePath}", processId);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusUI.ShowStatus($"Error executing Playwright test: {ex.Message}");
                await AgentService.SendOutputAsync($"Error executing Playwright test: {ex.Message}", processId);
                await AgentService.SendResultAsync($"Error:Playwright test execution failed:{ex.Message}", processId);
            }
            finally
            {
                // Clean up temp config
                if (tempConfigPath != null && File.Exists(tempConfigPath))
                {
                    try { File.Delete(tempConfigPath); } catch { /* ignore */ }
                }

                if (resultsGenerated)
                {
                    try
                    {
                        if (Directory.Exists(projectPath)) Directory.Delete(projectPath, true);
                        if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
                    }
                    catch (Exception ex)
                    {
                        StatusUI.ShowStatus($"Cleanup error: {ex.Message}");
                    }
                }
            }
        }

        private static string StripAnsiCodes(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Regex.Replace(text, @"\x1B\[[0-9;]*[a-zA-Z]|\x1B[=>]|\x1B\][\s\S]*?(\x07|\x1B\\)", string.Empty);
        }

        private static bool IsNonErrorStderr(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;
            if (Regex.IsMatch(line, @"^\[.+\]\s+(INFO|WARN|WARNING)\s+")) return true;
            if (Regex.IsMatch(line, @"^\w{3}\s+\d{1,2},\s+\d{4}\s+\d{1,2}:\d{2}:\d{2}\s+(AM|PM)")) return true;
            if (line.TrimStart().StartsWith("WARNING:") && !line.Contains("FAILURE") && !line.Contains("ERROR")) return true;
            if (Regex.IsMatch(line, @"^npm\s+(warn|notice|info)\s+", RegexOptions.IgnoreCase)) return true;
            return false;
        }
    }
}
