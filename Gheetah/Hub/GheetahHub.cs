using Gheetah.Helper;
using Gheetah.Interfaces;
using Gheetah.Models.AgentModel;
using Gheetah.Models.ProcessModel;
using Microsoft.AspNetCore.SignalR;

namespace Gheetah.Hub
{
    public class GheetahHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static bool _isInitialized = false;
        private readonly ITestResultProcessor _testResultProcessor;
        private readonly IFileService _fileService;
        private readonly IProcessService _processService;

        public GheetahHub(IFileService fileService, ITestResultProcessor testResultProcessor, IProcessService processService)
        {
            if (!_isInitialized)
            {
                Console.WriteLine("GheetahHub was launched.");
                _isInitialized = true;
            }

            _fileService = fileService;
            _testResultProcessor = testResultProcessor;
            _processService = processService;
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json");
            var agent = agents?.FirstOrDefault(a => a.ConnectionId == Context.ConnectionId);
            if (agent != null)
            {
                agent.Status = "offline";
                agent.Availability = "not available";
                agent.ConnectionId = null;
                await _fileService.SaveConfigAsync("agents-list.json", agents);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterAgent(AgentInfo agentInfo)
        {
            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
            var existingAgent = agents.FirstOrDefault(a => a.AgentId == agentInfo.AgentId);
            if (existingAgent != null && existingAgent.Status == "offline" && existingAgent.Availability == "declined")
            {
                await Clients.Client(agentInfo.ConnectionId).SendAsync("ReceiveStatus", "Your first request declined by Admin, please contact with Admin and ask a permission");
                Console.WriteLine($"RegisterAgent: AgentId {agentInfo.AgentId} previously rejected, new registration is blocked.");
                return;
            }

            var pendingAgents = await _fileService.LoadConfigAsync<List<AgentInfo>>("pending-agent-request-list.json") ?? new List<AgentInfo>();
            var existingPending = pendingAgents.FirstOrDefault(a => a.AgentId == agentInfo.AgentId);
            if (existingPending != null)
            {
                existingPending.ConnectionId = agentInfo.ConnectionId;
                existingPending.OS = agentInfo.OS;
                existingPending.EnvironmentName = agentInfo.EnvironmentName;
            }
            else
            {
                pendingAgents.Add(agentInfo);
            }
            await _fileService.SaveConfigAsync("pending-agent-request-list.json", pendingAgents);
            Console.WriteLine($"RegisterAgent was called: AgentId: {agentInfo.AgentId}, ConnectionId: {agentInfo.ConnectionId}");
            await Clients.Client(agentInfo.ConnectionId).SendAsync("ReceiveStatus", "Agent is already registered or pending");
        }

        public async Task Pong(string agentId, string connectionId, int runningScenarios)
        {
            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
            var existingAgent = agents.FirstOrDefault(a => a.AgentId == agentId);
            if (existingAgent != null)
            {
                if (existingAgent.Status == "offline" && existingAgent.Availability == "declined")
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveStatus", "Your first request declined by Admin, please contact with Admin and ask a permission");
                    Console.WriteLine($"Pong: AgentId {agentId} previously rejected.");
                    return;
                }
                existingAgent.ConnectionId = connectionId;
                existingAgent.Status = "online";
                existingAgent.Availability = runningScenarios < 4 ? "available" : "busy";
                await _fileService.SaveConfigAsync("agents-list.json", agents);
                Console.WriteLine($"Pong was received: AgentId: {agentId}, ConnectionId: {connectionId}, RunningScenarios: {runningScenarios}");
            }
            else
            {
                var pendingAgents = await _fileService.LoadConfigAsync<List<AgentInfo>>("pending-agent-request-list.json") ?? new List<AgentInfo>();
                var pendingAgent = pendingAgents.FirstOrDefault(a => a.AgentId == agentId);
                if (pendingAgent == null)
                {
                    pendingAgent = new AgentInfo
                    {
                        AgentId = agentId,
                        ConnectionId = connectionId,
                        OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                        EnvironmentName = Environment.MachineName,
                        Status = "pending",
                        Availability = "not available"
                    };
                    pendingAgents.Add(pendingAgent);
                    await _fileService.SaveConfigAsync("pending-agent-request-list.json", pendingAgents);
                    await Clients.Client(connectionId).SendAsync("ReceiveStatus", "Agent is already registered or pending");
                    Console.WriteLine($"Pong: AgentId {agentId} not found, added to pending list.");
                }
            }
        }

        public async Task UpdateAvailability(string agentId, string availability)
        {
            try
            {
                Console.WriteLine($"UpdateAvailability called - AgentId: {agentId}, Availability: {availability}, ConnectionId: {Context.ConnectionId}");
                var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
                var agent = agents.FirstOrDefault(a => a.AgentId == agentId);

                if (agent != null)
                {
                    if (agent.ConnectionId != Context.ConnectionId)
                    {
                        Console.WriteLine($"ConnectionId updating: {agent.ConnectionId} -> {Context.ConnectionId}");
                        agent.ConnectionId = Context.ConnectionId;
                    }
                    agent.Status = "online";
                    agent.Availability = availability;
                    Console.WriteLine($"Agent availability updated - AgentId: {agentId}, Availability: {availability}");
                }
                else
                {
                    var newAgent = new AgentInfo
                    {
                        AgentId = agentId,
                        ConnectionId = Context.ConnectionId,
                        Status = "online",
                        Availability = availability
                    };
                    agents.Add(newAgent);
                    Console.WriteLine($"New agent registered: AgentId: {agentId}, ConnectionId: {Context.ConnectionId}");
                }

                await _fileService.SaveConfigAsync("agents-list.json", agents);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateAvailability error - AgentId: {agentId}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
        }

        public async Task SendOutput(string agentId, string output, string processId)
        {
            try
            {
                Console.WriteLine($"SendOutput was called - AgentId: {agentId}, ProcessId: {processId}, Output length: {output?.Length}");
                if (output.StartsWith("TestResult:"))
                {
                    var content = output.Substring("TestResult:".Length);
                    var htmlReport = await GenerateHtmlReportFromContent(content);
                    Console.WriteLine($"Sending ReceiveHtmlReport - ProcessId: {processId}");
                    await Clients.Group(processId).SendAsync("ReceiveHtmlReport", htmlReport);
                }
                else
                {
                    Console.WriteLine($"Sending ReceiveOutput - ProcessId: {processId}");
                    await Clients.Group(processId).SendAsync("ReceiveOutput", output);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendOutput error - AgentId: {agentId}, ProcessId: {processId}, Error: {ex.Message}");
                throw;
            }
        }

        public async Task SendResult(string agentId, string result, string processId)
        {
            try
            {
                Console.WriteLine($"SendResult called - AgentId: {agentId}, ProcessId: {processId}, Result length: {result?.Length}");
                await Clients.Group(processId).SendAsync("ReceiveResult", result);
                if (result.StartsWith("TestResult:"))
                {
                    var content = result.Substring("TestResult:".Length);
                    var htmlReport = await GenerateHtmlReportFromContent(content);
                    Console.WriteLine($"Sending ReceiveHtmlReport - ProcessId: {processId}");
                    await Clients.Group(processId).SendAsync("ReceiveHtmlReport", htmlReport);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendResult error - AgentId: {agentId}, ProcessId: {processId}, Error: {ex.Message}");
                throw;
            }
        }

        public async Task SubscribeToProcess(string processId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, processId);
            Console.WriteLine($"Client {Context.ConnectionId} subscribed to process {processId}");
        }

        public async Task UnsubscribeFromProcess(string processId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, processId);
            Console.WriteLine($"Client {Context.ConnectionId} unsubscribed from process {processId}");
        }

        public async Task ReceiveOutput(string output, string processId)
        {
            Console.WriteLine($"Received output for ProcessId: {processId}, Output: {output}");
            await Clients.Group(processId).SendAsync("ReceiveOutput", output);
        }

        public async Task ReceiveResult(string result, string processId)
        {
            try
            {
                Console.WriteLine($"ReceiveResult called - ProcessId: {processId}, Result length: {result.Length}");
                await Clients.Group(processId).SendAsync("ReceiveOutput", result);

                if (result.StartsWith("TestResult:"))
                {
                    var content = result.Substring("TestResult:".Length);
                    var trimmed = content.TrimStart();
                    var ext = (trimmed.StartsWith("[") || trimmed.StartsWith("{")) ? ".json" : ".trx";
                    var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"testresults_{Guid.NewGuid()}{ext}");
                    await System.IO.File.WriteAllTextAsync(tempFile, content);

                    // Use the real ProcessInfo from ProcessService so HTML report is stored and accessible
                    // (controller creates ProcessInfo for agent runs before sending to agent)
                    var processInfo = _processService.GetProcess(processId)
                        ?? new ProcessInfo { Id = processId };

                    try
                    {
                        await _testResultProcessor.ProcessTestResultsAsync(processInfo, tempFile);
                        processInfo.Status = ProcessStatus.Executed;
                        Console.WriteLine($"Test results processed for processId: {processId}");
                    }
                    catch (Exception ex)
                    {
                        processInfo.Status = ProcessStatus.Failed;
                        Console.WriteLine($"Error processing test results for processId: {processId}, Error: {ex.Message}");
                        await Clients.Group(processId).SendAsync("ReceiveOutput", $"Error processing test results: {ex.Message}");
                    }
                    finally
                    {
                        if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
                    }
                    await Clients.Group(processId).SendAsync("ReceiveCompletionMessage", $"Process {processId} completed with test results.");
                }
                else if (result.StartsWith("Error:"))
                {
                    var errorMessage = result.Substring("Error:".Length);
                    Console.WriteLine($"Error received - ProcessId: {processId}, Error: {errorMessage}");
                    await Clients.Group(processId).SendAsync("ReceiveOutput", errorMessage);
                    await Clients.Group(processId).SendAsync("ReceiveCompletionMessage", $"Process failed: {errorMessage}");
                }
                else
                {
                    await Clients.Group(processId).SendAsync("ReceiveCompletionMessage", $"Process {processId} completed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReceiveResult error - ProcessId: {processId}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                await Clients.Group(processId).SendAsync("ReceiveOutput", $"Error processing result: {ex.Message}");
                await Clients.Group(processId).SendAsync("ReceiveCompletionMessage", $"Process failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects whether content is Cucumber JSON, Playwright JSON, or TRX XML,
        /// then generates the appropriate HTML report string.
        /// </summary>
        private static async Task<string> GenerateHtmlReportFromContent(string content)
        {
            var trimmed = content.TrimStart();
            var ext = (trimmed.StartsWith("[") || trimmed.StartsWith("{")) ? ".json" : ".trx";
            var tempFile = Path.Combine(Path.GetTempPath(), $"testresults_{Guid.NewGuid()}{ext}");
            try
            {
                await File.WriteAllTextAsync(tempFile, content);
                if (ext == ".json")
                {
                    return trimmed.StartsWith("[")
                        ? ScenarioHelper.GenerateCucumberHtmlReport(tempFile)
                        : ScenarioHelper.GeneratePlaywrightHtmlReport(tempFile);
                }
                else
                {
                    var steps = ScenarioHelper.ParseStdOutFromXml(tempFile);
                    return ScenarioHelper.GenerateHtmlReport(steps);
                }
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task NotifyCancellation(string processId)
        {
            await Clients.Group(processId).SendAsync("ProcessCancelled");
        }

        public async Task SendZipFile(string agentId, string processId, byte[] zipData)
        {
            try
            {
                Console.WriteLine($"SendZipFile was called - AgentId: {agentId}, ProcessId: {processId}");
                var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
                var agent = agents.FirstOrDefault(a => a.AgentId == agentId);

                if (agent == null || string.IsNullOrEmpty(agent.ConnectionId))
                {
                    Console.WriteLine($"Error: {agentId} the agent with ID was not found or is offline.");
                    return;
                }

                await Clients.Client(agent.ConnectionId).SendAsync("ReceiveZipFile", processId, zipData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendZipFile Error: {ex.Message}");
                throw;
            }
        }
    }
}