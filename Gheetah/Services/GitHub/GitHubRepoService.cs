using System.Net.Http.Headers;
using System.Text;
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

        public async Task<string> CreatePullRequestAsync(RepoSettingsVm settings, string sourceBranch, string targetBranch, string title, string description)
        {
            try
            {
                string owner = null;
                string repo = null;
                
                var repos = await GetReposAsync(settings);
                var matchingRepo = repos.FirstOrDefault(r => 
                    r.Name.Equals(settings.DisplayName, StringComparison.OrdinalIgnoreCase));
                
                if (matchingRepo == null || string.IsNullOrEmpty(matchingRepo.RemoteUrl))
                {
                    throw new Exception($"Repository '{settings.DisplayName}' not found on GitHub. " +
                                      "Please ensure the remote repository exists and matches the project name.");
                }

                var url = matchingRepo.RemoteUrl.Replace(".git", "");
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Trim('/').Split('/');
                
                if (segments.Length < 2)
                {
                    throw new Exception($"Invalid repository URL format: {matchingRepo.RemoteUrl}");
                }
                
                owner = segments[^2];
                repo = segments[^1];

                var request = new HttpRequestMessage(HttpMethod.Post, 
                    $"https://api.github.com/repos/{owner}/{repo}/pulls");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
                request.Headers.UserAgent.ParseAdd("Gheetah-IDE");
                
                var payload = new
                {
                    title = title,
                    head = sourceBranch,
                    @base = targetBranch,
                    body = description
                };
                
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var resultContent = await response.Content.ReadAsStringAsync();
                    using var resultJson = JsonDocument.Parse(resultContent);
                    return resultJson.RootElement.GetProperty("html_url").GetString();
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                
                if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity && 
                    errorContent.Contains("pull request already exists"))
                {
                    return null;
                }
                
                throw new Exception($"GitHub PR creation failed ({response.StatusCode}): {errorContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreatePullRequestAsync error: {ex.Message}");
                throw;
            }
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

        public async Task<string> CreateRepositoryAsync(RepoSettingsVm settings, string repoName)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/user/repos");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
            request.Headers.UserAgent.ParseAdd("Gheetah-IDE");
    
            var payload = new
            {
                name = repoName,
                @private = false,
                auto_init = false,
                description = "Created by Gheetah IDE"
            };
    
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
    
            var response = await _httpClient.SendAsync(request);
    
            if (response.IsSuccessStatusCode)
            {
                var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return result.RootElement.GetProperty("clone_url").GetString();
            }
    
            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                throw new Exception("Repository already exists on GitHub.");
            }
    
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"GitHub API error ({response.StatusCode}): {errorContent}");
        }

    }
}
