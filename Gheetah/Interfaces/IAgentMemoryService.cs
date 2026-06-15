using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IAgentMemoryService
    {
        Task<AgentMemory> GetMemoryAsync(string projectId, string projectFolderPath);
        Task SaveMemoryAsync(AgentMemory memory, string projectFolderPath);
        Task UpdateAfterSessionAsync(string projectId, string projectFolderPath,
            List<string> newScenarioTitles, List<string> testedAreas, string sessionSummary);
    }
}
