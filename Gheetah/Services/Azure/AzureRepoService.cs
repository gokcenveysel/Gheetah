using Gheetah.Interfaces;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Gheetah.Services.Azure
{
    public class AzureRepoService : IGitRepoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public bool IsMatch(string repoType) => repoType == "Azure";

        public AzureRepoService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<List<GitRepoVm>> GetReposAsync(RepoSettingsVm setting)
        {
            var client = _httpClientFactory.CreateClient();
            var byteArray = Encoding.ASCII.GetBytes($"{setting.Username}:{setting.AccessToken}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var repoUrl = $"https://dev.azure.com/{setting.DomainName}/{setting.ProjectName}/_apis/git/repositories?api-version=7.0";
            var repoResponse = await client.GetAsync(repoUrl);
            repoResponse.EnsureSuccessStatusCode();

            var repoContent = await repoResponse.Content.ReadAsStringAsync();
            var repoJson = JsonDocument.Parse(repoContent);

            var repoList = new List<GitRepoVm>();
            
            var tasks = new List<Task<GitRepoVm>>();

            foreach (var repo in repoJson.RootElement.GetProperty("value").EnumerateArray())
            {
                var name = repo.GetProperty("name").GetString();
                var remoteUrl = repo.GetProperty("remoteUrl").GetString();
                var id = repo.GetProperty("id").GetString();

                tasks.Add(ProcessRepositoryAsync(client, setting, name, remoteUrl, id));
            }
            var results = await Task.WhenAll(tasks);
            repoList.AddRange(results);

            return repoList;
        }

        private async Task<GitRepoVm> ProcessRepositoryAsync(HttpClient client, RepoSettingsVm setting, string name, string remoteUrl, string id)
        {
            string language = "Unknown";

            try
            {
                var itemsUrl =
                    $"https://dev.azure.com/{setting.DomainName}/{setting.ProjectName}/_apis/git/repositories/{id}/items?recursionLevel=Full&api-version=7.0";
                var itemsResponse = await client.GetAsync(itemsUrl);

                if (itemsResponse.IsSuccessStatusCode)
                {
                    var itemsContent = await itemsResponse.Content.ReadAsStringAsync();
                    var itemsJson = JsonDocument.Parse(itemsContent);

                    var files = itemsJson.RootElement.GetProperty("value")
                        .EnumerateArray()
                        .Where(x => x.GetProperty("gitObjectType").GetString() == "blob")
                        .Select(x => x.GetProperty("path").GetString())
                        .ToList();

                    language = DetectLanguageFromFiles(files);
                }
            }
            catch
            {
                
            }

            return new GitRepoVm
            {
                Name = name,
                RemoteUrl = remoteUrl,
                Language = language
            };
        }

        private string DetectLanguageFromFiles(List<string> filePaths)
        {
            var extensions = filePaths
                .Select(f => Path.GetExtension(f).ToLowerInvariant())
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .ToList();

            if (extensions.Any(e => e == ".cs" || e == ".csproj"))
                return "C#";
            if (extensions.Any(e => e == ".py"))
                return "Python";
            if (extensions.Any(e => e == ".js" || e == ".ts" || e == ".jsx" || e == ".tsx"))
                return "JavaScript/TypeScript";
            if (extensions.Any(e => e == ".java" || e == ".gradle" || e == ".xml"))
                return "Java";
            if (extensions.Any(e => e == ".cpp" || e == ".h" || e == ".c"))
                return "C/C++";
            if (extensions.Any(e => e == ".rb"))
                return "Ruby";
            if (extensions.Any(e => e == ".go"))
                return "Go";
            if (extensions.Any(e => e == ".php"))
                return "PHP";
            if (extensions.Any(e => e == ".html" || e == ".css"))
                return "HTML/CSS";

            return "Unknown";
        }
    }
}