using Gheetah.Interfaces;
using Gheetah.Models.AiModels;

namespace Gheetah.Services
{
    public class AiScenarioService : IAiScenarioService
    {
        private readonly IFileService _fileService;

        public AiScenarioService(IFileService fileService)
        {
            _fileService = fileService;
        }

        private static string FileName(string projectId) => $"ai-scenarios-{projectId}.json";

        public async Task<List<AiScenario>> GetScenariosForProjectAsync(string projectId)
            => await _fileService.LoadConfigAsync<List<AiScenario>>(FileName(projectId)) ?? new List<AiScenario>();

        public async Task<AiScenario> GetByIdAsync(string scenarioId, string projectId)
        {
            var scenarios = await GetScenariosForProjectAsync(projectId);
            return scenarios.FirstOrDefault(s => s.Id == scenarioId);
        }

        public async Task SaveScenarioAsync(AiScenario scenario)
        {
            // Persist .feature file to project folder when FilePath is set
            if (!string.IsNullOrEmpty(scenario.FilePath) && !string.IsNullOrEmpty(scenario.GherkinContent))
            {
                var dir = Path.GetDirectoryName(scenario.FilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(scenario.FilePath, scenario.GherkinContent);
            }

            // Always keep JSON index (lightweight metadata)
            var scenarios = await GetScenariosForProjectAsync(scenario.ProjectId);
            var existing = scenarios.FirstOrDefault(s => s.Id == scenario.Id);
            if (existing != null)
                scenarios.Remove(existing);
            scenarios.Add(scenario);
            await _fileService.SaveConfigAsync(FileName(scenario.ProjectId), scenarios);
        }

        public async Task DeleteScenarioAsync(string scenarioId, string projectId)
        {
            var scenarios = await GetScenariosForProjectAsync(projectId);
            var scenario = scenarios.FirstOrDefault(s => s.Id == scenarioId);
            if (scenario != null)
            {
                // Remove .feature file from disk if it exists
                if (!string.IsNullOrEmpty(scenario.FilePath) && File.Exists(scenario.FilePath))
                    File.Delete(scenario.FilePath);

                scenarios.Remove(scenario);
                await _fileService.SaveConfigAsync(FileName(projectId), scenarios);
            }
        }
    }
}
