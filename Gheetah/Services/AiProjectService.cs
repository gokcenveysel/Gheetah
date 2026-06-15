using System.Text.Json;
using Gheetah.Interfaces;
using Gheetah.Models.AiModels;

namespace Gheetah.Services
{
    public class AiProjectService : IAiProjectService
    {
        private readonly IFileService _fileService;
        private const string FileName = "ai-projects.json";

        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        public AiProjectService(IFileService fileService)
        {
            _fileService = fileService;
        }

        public async Task<List<AiProject>> GetProjectsAsync()
            => await _fileService.LoadConfigAsync<List<AiProject>>(FileName) ?? new List<AiProject>();

        public async Task<AiProject> GetByIdAsync(string id)
        {
            var projects = await GetProjectsAsync();
            return projects.FirstOrDefault(p => p.Id == id);
        }

        public async Task SaveProjectAsync(AiProject project)
        {
            // Create project folder structure on first save
            if (!string.IsNullOrEmpty(project.FolderPath) && !Directory.Exists(project.FolderPath))
            {
                Directory.CreateDirectory(Path.Combine(project.FolderPath, "scenarios"));
                Directory.CreateDirectory(Path.Combine(project.FolderPath, "session-history"));
                await File.WriteAllTextAsync(
                    Path.Combine(project.FolderPath, ".gheetah-ai.json"),
                    JsonSerializer.Serialize(new
                    {
                        projectId = project.Id,
                        name = project.Name,
                        createdDate = project.CreatedDate
                    }, JsonOpts));
            }

            var projects = await GetProjectsAsync();
            var existing = projects.FirstOrDefault(p => p.Id == project.Id);
            if (existing != null)
                projects.Remove(existing);
            projects.Add(project);
            await _fileService.SaveConfigAsync(FileName, projects);
        }

        public async Task DeleteProjectAsync(string id)
        {
            var projects = await GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == id);
            if (project != null)
            {
                projects.Remove(project);
                await _fileService.SaveConfigAsync(FileName, projects);
            }

            // Remove scenario file for this project
            var scenarioFile = $"ai-scenarios-{id}.json";
            if (await _fileService.ConfigExistsAsync(scenarioFile))
            {
                var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", scenarioFile);
                await _fileService.DeleteAsync(dataPath);
            }
        }
    }
}
