using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IAiAgentService
    {
        Task<List<AiAgent>> GetAgentsAsync();
        Task<AiAgent> GetByIdAsync(string id);
        Task SaveAgentAsync(AiAgent agent);
        Task DeleteAgentAsync(string id);
        Task<bool> TestConnectionAsync(string agentId);
        Task<AiAgent> GetDefaultAgentAsync();
    }
}
