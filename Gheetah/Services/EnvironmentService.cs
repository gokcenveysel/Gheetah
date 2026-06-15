using Gheetah.Interfaces;
using Gheetah.Models.AiModels;

namespace Gheetah.Services
{
    public class EnvironmentService : IEnvironmentService
    {
        private readonly IFileService _fileService;
        private const string FileName = "ai-environments.json";

        public EnvironmentService(IFileService fileService)
        {
            _fileService = fileService;
        }

        public async Task<List<EnvironmentConfig>> GetEnvironmentsAsync(string projectId = null)
        {
            var all = await _fileService.LoadConfigAsync<List<EnvironmentConfig>>(FileName) ?? new List<EnvironmentConfig>();
            return projectId == null ? all : all.Where(e => e.ProjectId == projectId).ToList();
        }

        public async Task<EnvironmentConfig> GetByIdAsync(string id)
        {
            var all = await _fileService.LoadConfigAsync<List<EnvironmentConfig>>(FileName) ?? new List<EnvironmentConfig>();
            return all.FirstOrDefault(e => e.Id == id);
        }

        public async Task<EnvironmentConfig> GetDefaultForProjectAsync(string projectId)
        {
            var envs = await GetEnvironmentsAsync(projectId);
            return envs.FirstOrDefault(e => e.IsDefault) ?? envs.FirstOrDefault();
        }

        public async Task SaveEnvironmentAsync(EnvironmentConfig env)
        {
            var all = await _fileService.LoadConfigAsync<List<EnvironmentConfig>>(FileName) ?? new List<EnvironmentConfig>();

            if (env.IsDefault)
            {
                foreach (var e in all.Where(e => e.ProjectId == env.ProjectId))
                    e.IsDefault = false;
            }

            env.UpdatedDate = DateTime.UtcNow;
            var existing = all.FirstOrDefault(e => e.Id == env.Id);
            if (existing != null)
                all.Remove(existing);
            all.Add(env);
            await _fileService.SaveConfigAsync(FileName, all);
        }

        public async Task DeleteEnvironmentAsync(string id)
        {
            var all = await _fileService.LoadConfigAsync<List<EnvironmentConfig>>(FileName) ?? new List<EnvironmentConfig>();
            var env = all.FirstOrDefault(e => e.Id == id);
            if (env != null)
            {
                all.Remove(env);
                await _fileService.SaveConfigAsync(FileName, all);
            }
        }

        public async Task<Dictionary<string, string>> ResolveVariablesAsync(string envId)
        {
            var env = await GetByIdAsync(envId);
            return env?.Variables ?? new Dictionary<string, string>();
        }
    }
}
