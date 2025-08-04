using Gheetah.Interfaces;
using Gheetah.Models.AgentModel;

namespace Gheetah.Services;

public class AgentManager
{
    private readonly IFileService _fileService;
    private const string PendingFile = "pending-agent-request-list.json";
    private const string BlacklistFile = "black-agent-list.json";

    public AgentManager(IFileService fileService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    }

    public async Task AddPendingAgentAsync(AgentInfo agentInfo)
    {
        var pendingAgents = await _fileService.LoadConfigAsync<List<AgentInfo>>(PendingFile) ?? new List<AgentInfo>();
        pendingAgents.Add(agentInfo);
        await _fileService.SaveConfigAsync(PendingFile, pendingAgents);
    }

    public async Task AcceptAgentAsync(string agentId)
    {
        var pendingAgents = await _fileService.LoadConfigAsync<List<AgentInfo>>("pending-agent-request-list.json") ?? new List<AgentInfo>();
        var agent = pendingAgents.FirstOrDefault(a => a.AgentId == agentId);
        if (agent != null)
        {
            pendingAgents.Remove(agent);
            await _fileService.SaveConfigAsync("pending-agent-request-list.json", pendingAgents);

            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
            agent.Status = "online";
            agent.Availability = "available";
            agents.Add(agent);
            await _fileService.SaveConfigAsync("agents-list.json", agents);
        }
    }

    public async Task DeclineAgentAsync(string agentId)
    {
        var pendingAgents = await _fileService.LoadConfigAsync<List<AgentInfo>>(PendingFile) ?? new List<AgentInfo>();
        var agent = pendingAgents.FirstOrDefault(a => a.AgentId == agentId);
        if (agent != null)
        {
            pendingAgents.Remove(agent);
            await _fileService.SaveConfigAsync(PendingFile, pendingAgents);

            var blacklist = await _fileService.LoadConfigAsync<List<AgentInfo>>(BlacklistFile) ?? new List<AgentInfo>();
            blacklist.Add(agent);
            await _fileService.SaveConfigAsync(BlacklistFile, blacklist);
        }
    }
}

