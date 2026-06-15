using System.Text.Json;
using Gheetah.Interfaces;
using Gheetah.Models.AiModels;

namespace Gheetah.Services
{
    public class AgentMemoryService : IAgentMemoryService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static string MemoryPath(string projectFolderPath)
            => Path.Combine(projectFolderPath, "agent-memory.json");

        public async Task<AgentMemory> GetMemoryAsync(string projectId, string projectFolderPath)
        {
            var path = MemoryPath(projectFolderPath);
            if (!File.Exists(path))
                return new AgentMemory { ProjectId = projectId };

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<AgentMemory>(json, JsonOpts) ?? new AgentMemory { ProjectId = projectId };
        }

        public async Task SaveMemoryAsync(AgentMemory memory, string projectFolderPath)
        {
            memory.LastUpdated = DateTime.UtcNow;
            await File.WriteAllTextAsync(MemoryPath(projectFolderPath), JsonSerializer.Serialize(memory, JsonOpts));
        }

        public async Task UpdateAfterSessionAsync(string projectId, string projectFolderPath,
            List<string> newScenarioTitles, List<string> testedAreas, string sessionSummary)
        {
            var memory = await GetMemoryAsync(projectId, projectFolderPath);

            foreach (var title in newScenarioTitles)
                if (!memory.GeneratedScenarioTitles.Contains(title))
                    memory.GeneratedScenarioTitles.Add(title);

            foreach (var area in testedAreas)
                if (!memory.TestedAreas.Contains(area))
                    memory.TestedAreas.Add(area);

            if (!string.IsNullOrEmpty(sessionSummary))
                memory.LastSessionSummary = sessionSummary;

            await SaveMemoryAsync(memory, projectFolderPath);
        }
    }
}
