using Gheetah.Interfaces;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;
using System.Text.Json;

namespace Gheetah.Services.GitHub
{
    public class GitHubRepoService : IGitRepoService
    {
        private readonly HttpClient _httpClient;

        public GitHubRepoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public bool IsMatch(string providerType) => providerType.Equals("GitHub", StringComparison.OrdinalIgnoreCase);

        public async Task<List<GitRepoVm>> GetReposAsync(RepoSettingsVm setting)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/repos");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", setting.AccessToken);
            request.Headers.UserAgent.ParseAdd("GheetahApp");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var repos = doc.RootElement.EnumerateArray()
                .Select(repo => new GitRepoVm
                {
                    Name = repo.GetProperty("name").GetString(),
                    RemoteUrl = repo.TryGetProperty("clone_url", out var cloneUrlProp) ? cloneUrlProp.GetString() : null,
                    Language = repo.TryGetProperty("language", out var langProp) ? langProp.GetString() : "Unknown"
                }).ToList();

            return repos;
        }

    }
}
