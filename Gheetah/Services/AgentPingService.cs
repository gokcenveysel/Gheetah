using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.AgentModel;
using Hangfire;
using Microsoft.AspNetCore.SignalR;

namespace Gheetah.Services
{
    public class AgentPingService
    {
        private readonly IHubContext<GheetahHub> _hubContext;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IFileService _fileService;

        public AgentPingService(IHubContext<GheetahHub> hubContext, IBackgroundJobClient backgroundJobClient, IFileService fileService)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            _fileService = fileService;
        }

        public async Task PingAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                Console.WriteLine("Error: agentId is null or empty.");
                return;
            }

            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json");
            var agent = agents?.FirstOrDefault(a => a.AgentId == agentId);
            if (agent == null)
            {
                Console.WriteLine($"Error: Agent with ID {agentId} not found.");
                return;
            }

            if (string.IsNullOrEmpty(agent.ConnectionId))
            {
                Console.WriteLine($"Warning: ConnectionId of agent with ID {agentId} is null, the agent is offline.");
                agent.Status = "offline";
                agent.Availability = "not available";
                await _fileService.SaveConfigAsync("agents-list.json", agents);
                return;
            }

            var client = _hubContext.Clients.Client(agent.ConnectionId);
            if (client != null)
            {
                try
                {
                    Console.WriteLine($"Sending ping: AgentId: {agentId}, ConnectionId: {agent.ConnectionId}");
                    await client.SendAsync("Ping");
                    agent.Status = "online";
                    await _fileService.SaveConfigAsync("agents-list.json", agents);
                    _backgroundJobClient.Schedule(() => PingAgent(agentId), TimeSpan.FromSeconds(30));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending ping: {ex.Message}");
                    agent.Status = "offline";
                    agent.Availability = "not available";
                    agent.ConnectionId = null;
                    await _fileService.SaveConfigAsync("agents-list.json", agents);
                }
            }
            else
            {
                Console.WriteLine($"Warning: ConnectionId ({agent.ConnectionId}) of agent with ID {agentId} could not be found, the agent is offline.");
                agent.Status = "offline";
                agent.Availability = "not available";
                agent.ConnectionId = null;
                await _fileService.SaveConfigAsync("agents-list.json", agents);
            }
        }
    }
}
