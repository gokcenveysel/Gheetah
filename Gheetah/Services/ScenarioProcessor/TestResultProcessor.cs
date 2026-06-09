using Gheetah.Helper;
using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.ProcessModel;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Gheetah.Services.ScenarioProcessor
{
    public class TestResultProcessor : ITestResultProcessor
    {
        private readonly IHubContext<GheetahHub> _hubContext;

        public TestResultProcessor(IHubContext<GheetahHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task ProcessTestResultsAsync(ProcessInfo processInfo, string testResultsFilePath)
        {
            if (File.Exists(testResultsFilePath))
            {
                try
                {
                    string partialReport;
                    if (testResultsFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        partialReport = IsCucumberJson(testResultsFilePath)
                            ? ScenarioHelper.GenerateCucumberHtmlReport(testResultsFilePath)
                            : ScenarioHelper.GeneratePlaywrightHtmlReport(testResultsFilePath);
                    }
                    else
                    {
                        var steps = ScenarioHelper.ParseStdOutFromXml(testResultsFilePath);
                        partialReport = ScenarioHelper.GenerateHtmlReport(steps);
                    }

                    processInfo.HtmlReport += partialReport;
                    await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveHtmlReport", processInfo.HtmlReport);

                    // Send authoritative test outcome so the client badge reflects the real result.
                    // This is sent BEFORE ReceiveCompletionMessage so the badge is correct by the time
                    // the completion handler fires.
                    var outcome = DetermineOutcome(partialReport);
                    await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveTestResult", outcome);
                }
                catch (Exception ex)
                {
                    processInfo.Output.Add($"Report generation error: {ex.Message}");
                    await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput",
                        $"Error generating report: {ex.Message}");
                }
            }
            else
            {
                processInfo.Output.Add("Test results file not found.");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput",
                    "Test results file not found.");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveTestResult", "Skipped");
            }
        }

        /// <summary>
        /// Determines pass/fail/skipped from the generated HTML badge counts.
        /// Works for TRX, Cucumber JSON, and Playwright JSON reports uniformly.
        /// </summary>
        private static string DetermineOutcome(string htmlReport)
        {
            if (string.IsNullOrEmpty(htmlReport)) return "Skipped";
            var failedMatch = Regex.Match(htmlReport, @"<span class='badge failed'>(\d+)");
            var passedMatch = Regex.Match(htmlReport, @"<span class='badge passed'>(\d+)");
            int failed = failedMatch.Success ? int.Parse(failedMatch.Groups[1].Value) : 0;
            int passed = passedMatch.Success ? int.Parse(passedMatch.Groups[1].Value) : 0;
            if (failed > 0) return "Failed";
            if (passed > 0) return "Passed";
            return "Skipped";
        }
        private static bool IsCucumberJson(string path)
        {
            try
            {
                return (File.ReadAllText(path).TrimStart().FirstOrDefault()) == '[';
            }
            catch { return false; }
        }
    }
}
