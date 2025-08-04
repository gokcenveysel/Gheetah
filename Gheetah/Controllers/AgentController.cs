using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.AgentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Gheetah.Controllers;

[Authorize(Policy = "Dynamic_admin-perm")]
public class AgentsController : Controller
{
    private readonly IHubContext<GheetahHub> _hubContext;
    private readonly IFileService _fileService;

    public AgentsController(IHubContext<GheetahHub> hubContext, IFileService fileService)
    {
        _hubContext = hubContext;
        _fileService = fileService;
    }

    public async Task<IActionResult> PendingRequests()
    {
        var pendingAgents = await _fileService.LoadConfigAsync<List<AgentInfo>>("pending-agent-request-list.json") ?? new List<AgentInfo>();
        return View(pendingAgents);
    }

    public async Task<IActionResult> List()
    {
        var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
        return View(agents);
    }

    [HttpPost]
    public async Task<IActionResult> Accept(string agentId)
    {
        try
        {
            var pendingAgents = await _fileService.LoadConfigAsync<List<AgentInfo>>("pending-agent-request-list.json") ?? new List<AgentInfo>();
            var agent = pendingAgents.FirstOrDefault(a => a.AgentId == agentId);
            if (agent == null)
            {
                Console.WriteLine($"Error: Agent Id {agentId} not found.");
                return NotFound($"AgentId {agentId} not found.");
            }

            pendingAgents.Remove(agent);
            await _fileService.SaveConfigAsync("pending-agent-request-list.json", pendingAgents);

            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
            var existingAgent = agents.FirstOrDefault(a => a.AgentId == agentId);
            if (existingAgent != null)
            {
                existingAgent.Status = "online";
                existingAgent.Availability = "available";
                existingAgent.ConnectionId = agent.ConnectionId;
                existingAgent.OS = agent.OS;
                existingAgent.EnvironmentName = agent.EnvironmentName;
            }
            else
            {
                agent.Status = "online";
                agent.Availability = "available";
                agents.Add(agent);
            }
            await _fileService.SaveConfigAsync("agents-list.json", agents);

            if (!string.IsNullOrEmpty(agent.ConnectionId))
            {
                await _hubContext.Clients.Client(agent.ConnectionId).SendAsync("ReceiveStatus", "Agent registered successfully");
                Console.WriteLine($"Agent accepted! AgentId: {agentId}, ConnectionId: {agent.ConnectionId}");
            }
            else
            {
                Console.WriteLine($"Error: ConnectionId not found for AgentId {agentId}.");
            }

            return RedirectToAction("PendingRequests");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Accept agent error: {ex.Message}");
            return StatusCode(500, "Agent acceptance failed.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Decline(string agentId)
    {
        try
        {
            var pendingAgents = await _fileService.LoadConfigAsync<List<AgentInfo>>("pending-agent-request-list.json") ?? new List<AgentInfo>();
            var agent = pendingAgents.FirstOrDefault(a => a.AgentId == agentId);
            if (agent == null)
            {
                Console.WriteLine($"Error: Agent Id {agentId} not found.");
                return NotFound($"AgentId {agentId} not found.");
            }

            var connectionId = agent.ConnectionId;
            Console.WriteLine($"Decline process begins! AgentId: {agentId}, ConnectionId: {connectionId}");

            pendingAgents.Remove(agent);
            await _fileService.SaveConfigAsync("pending-agent-request-list.json", pendingAgents);

            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
            var existingAgent = agents.FirstOrDefault(a => a.AgentId == agentId);
            if (existingAgent != null)
            {
                existingAgent.Status = "offline";
                existingAgent.Availability = "declined";
                existingAgent.ConnectionId = null;
            }
            else
            {
                agent.Status = "offline";
                agent.Availability = "declined";
                agent.ConnectionId = null;
                agents.Add(agent);
            }
            await _fileService.SaveConfigAsync("agents-list.json", agents);

            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", "Your register request declined by Admin, please contact with Admin");
                Console.WriteLine($"Agent rejected! AgentId: {agentId}, ConnectionId: {connectionId}");
            }
            else
            {
                Console.WriteLine($"Error: ConnectionId not found for AgentId {agentId}, message could not be sent.");
            }

            return RedirectToAction("PendingRequests");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decline agent error: {ex.Message}");
            return StatusCode(500, "Agent decline failed.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string agentId)
    {
        try
        {
            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
            var agent = agents.FirstOrDefault(a => a.AgentId == agentId);
            if (agent == null)
            {
                Console.WriteLine($"Error: Agent Id {agentId} not found.");
                return NotFound($"AgentId {agentId} not found.");
            }

            var connectionId = agent.ConnectionId;
            agents.Remove(agent);
            await _fileService.SaveConfigAsync("agents-list.json", agents);

            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", "Agent deleted by Admin");
                Console.WriteLine($"Agent removed! AgentId: {agentId}, ConnectionId: {connectionId}");
            }
            else
            {
                Console.WriteLine($"Error: ConnectionId not found for AgentId {agentId}.");
            }

            return RedirectToAction("List");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete agent error: {ex.Message}");
            return StatusCode(500, "Agent deletion failed.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableAgents()
    {
        try
        {
            var agents = await _fileService.LoadConfigAsync<List<AgentInfo>>("agents-list.json") ?? new List<AgentInfo>();
            var availableAgents = agents.Where(a => a.Status == "online" && a.Availability == "available").ToList();
            return Json(availableAgents);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetAvailableAgents error: {ex.Message}");
            return StatusCode(500, "Failed to load available agents.");
        }
    }

    [HttpGet]
    public IActionResult Download()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Download", "GheetahAgentSetup.exe");
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Agent installer not found.");
        }
        return File(System.IO.File.ReadAllBytes(filePath), "application/exe", "GheetahAgentSetup.exe");
    }
}