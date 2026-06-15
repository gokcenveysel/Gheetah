using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Gheetah.Services.AiAdapters
{
    public class GeminiAgentAdapter : IAiAgentAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeminiAgentAdapter> _logger;

        public string ProviderType => "Gemini";

        public GeminiAgentAdapter(IHttpClientFactory httpClientFactory, ILogger<GeminiAgentAdapter> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> IsHealthyAsync(AiAgent agent)
        {
            try
            {
                var client = CreateClient(agent);
                var model = agent.ModelName ?? "gemini-1.5-pro";
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}?key={agent.ApiKey}";
                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini health check failed for agent {AgentId}", agent.Id);
                return false;
            }
        }

        public async Task<string> GenerateAsync(AiAgent agent, string prompt, CancellationToken ct = default)
        {
            var client = CreateClient(agent);
            var model = agent.ModelName ?? "gemini-1.5-pro";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={agent.ApiKey}";
            var body = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            return doc.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
        }

        public async Task<bool> InitializeSessionAsync(AiAgent agent, GaapMessage initMessage)
        {
            await Task.CompletedTask;
            return true;
        }

        public async IAsyncEnumerable<GaapMessage> StreamOutputAsync(AiAgent agent, GaapMessage request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(request.Payload);
            var client = CreateClient(agent);
            var model = agent.ModelName ?? "gemini-1.5-pro";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?key={agent.ApiKey}";

            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = payload.GetProperty("gherkinContent").GetString() } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                yield return new GaapMessage
                {
                    MessageType = GaapMessageType.Error,
                    SessionId = request.SessionId,
                    Payload = $"HTTP {(int)response.StatusCode}"
                };
                yield break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line)) continue;

                yield return new GaapMessage
                {
                    MessageType = GaapMessageType.OutputChunk,
                    SessionId = request.SessionId,
                    Payload = line
                };
            }

            yield return new GaapMessage
            {
                MessageType = GaapMessageType.ScenarioComplete,
                SessionId = request.SessionId,
                Payload = "{}"
            };
        }

        public async Task TerminateSessionAsync(AiAgent agent, string sessionId)
            => await Task.CompletedTask;

        private HttpClient CreateClient(AiAgent agent)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(agent.TimeoutSeconds > 0 ? agent.TimeoutSeconds : 120);
            return client;
        }
    }
}
