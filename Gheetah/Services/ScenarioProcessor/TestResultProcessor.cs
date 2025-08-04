using Gheetah.Helper;
using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.ProcessModel;
using Microsoft.AspNetCore.SignalR;

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
                    var steps = ScenarioHelper.ParseStdOutFromXml(testResultsFilePath);
                    var partialReport = ScenarioHelper.GenerateHtmlReport(steps);
            
                    processInfo.HtmlReport += partialReport;
            
                    await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveHtmlReport", processInfo.HtmlReport);
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
            }
        }
    }
}
