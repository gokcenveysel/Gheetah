using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Gheetah.Services.AiAdapters;

namespace Gheetah.Services
{
    public class AiAgentService : IAiAgentService
    {
        private readonly IFileService _fileService;
        private readonly AiAgentAdapterFactory _adapterFactory;
        private const string FileName = "ai-agents.json";

        public AiAgentService(IFileService fileService, AiAgentAdapterFactory adapterFactory)
        {
            _fileService = fileService;
            _adapterFactory = adapterFactory;
        }

        public async Task<List<AiAgent>> GetAgentsAsync()
            => await _fileService.LoadConfigAsync<List<AiAgent>>(FileName) ?? new List<AiAgent>();

        public async Task<AiAgent> GetByIdAsync(string id)
        {
            var agents = await GetAgentsAsync();
            return agents.FirstOrDefault(a => a.Id == id);
        }

        public async Task<AiAgent> GetDefaultAgentAsync()
        {
            var agents = await GetAgentsAsync();
            return agents.FirstOrDefault(a => a.IsDefault && a.IsEnabled)
                ?? agents.FirstOrDefault(a => a.IsEnabled);
        }

        public async Task SaveAgentAsync(AiAgent agent)
        {
            var agents = await GetAgentsAsync();

            if (agent.IsDefault)
            {
                foreach (var a in agents)
                    a.IsDefault = false;
            }

            var existing = agents.FirstOrDefault(a => a.Id == agent.Id);
            if (existing != null)
                agents.Remove(existing);

            agents.Add(agent);
            await _fileService.SaveConfigAsync(FileName, agents);
        }

        public async Task DeleteAgentAsync(string id)
        {
            var agents = await GetAgentsAsync();
            var agent = agents.FirstOrDefault(a => a.Id == id);
            if (agent != null)
            {
                agents.Remove(agent);
                await _fileService.SaveConfigAsync(FileName, agents);
            }
        }

        public async Task<bool> TestConnectionAsync(string agentId)
        {
            var agent = await GetByIdAsync(agentId);
            if (agent == null) return false;

            try
            {
                var adapter = _adapterFactory.Create(agent.ProviderType.ToString());
                var healthy = await adapter.IsHealthyAsync(agent);

                agent.LastHealthCheckDate = DateTime.UtcNow;
                agent.LastHealthCheckStatus = healthy ? "ok" : "error";
                await SaveAgentAsync(agent);

                return healthy;
            }
            catch
            {
                agent.LastHealthCheckDate = DateTime.UtcNow;
                agent.LastHealthCheckStatus = "error";
                await SaveAgentAsync(agent);
                return false;
            }
        }
    }
}
