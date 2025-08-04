using Gheetah.Interfaces;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;
using System.Text.Json;

namespace Gheetah.Services.GitLab
{
    public class GitLabRepoService : IGitRepoService
    {
        private readonly HttpClient _httpClient;

        public GitLabRepoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public bool IsMatch(string providerType) => providerType.Equals("GitLab", StringComparison.OrdinalIgnoreCase);

        public async Task<List<GitRepoVm>> GetReposAsync(RepoSettingsVm setting)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://gitlab.com/api/v4/projects?membership=true");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", setting.AccessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var repos = doc.RootElement.EnumerateArray()
                .Select(repo => new GitRepoVm
                {
                    Name = repo.GetProperty("name").GetString(),
                    RemoteUrl = repo.GetProperty("remoteUrl").GetString(),
                    Language = repo.GetProperty("language").GetString()
                }).ToList();

            return repos;
        }
    }

}
