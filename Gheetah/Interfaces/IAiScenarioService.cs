using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IAiScenarioService
    {
        Task<List<AiScenario>> GetScenariosForProjectAsync(string projectId);
        Task<AiScenario> GetByIdAsync(string scenarioId, string projectId);
        Task SaveScenarioAsync(AiScenario scenario);
        Task DeleteScenarioAsync(string scenarioId, string projectId);
    }
}
