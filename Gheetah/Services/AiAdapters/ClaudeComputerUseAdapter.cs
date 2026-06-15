using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Gheetah.Services.AiAdapters
{
    public class ClaudeComputerUseAdapter : IAiAgentAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ClaudeComputerUseAdapter> _logger;

        public string ProviderType => "Claude";

        public ClaudeComputerUseAdapter(IHttpClientFactory httpClientFactory, ILogger<ClaudeComputerUseAdapter> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> IsHealthyAsync(AiAgent agent)
        {
            try
            {
                var client = CreateClient(agent);
                var endpoint = string.IsNullOrEmpty(agent.ApiEndpoint)
                    ? "https://api.anthropic.com/v1/messages"
                    : agent.ApiEndpoint.TrimEnd('/') + "/health";

                var response = await client.GetAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Claude health check failed for agent {AgentId}", agent.Id);
                return false;
            }
        }

        public async Task<string> GenerateAsync(AiAgent agent, string prompt, CancellationToken ct = default)
        {
            var client = CreateClient(agent);
            var endpoint = string.IsNullOrEmpty(agent.ApiEndpoint)
                ? "https://api.anthropic.com/v1/messages"
                : agent.ApiEndpoint;

            var body = new
            {
                model = agent.ModelName ?? "claude-sonnet-4-6",
                max_tokens = 4096,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            return doc.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        }

        public async Task<bool> InitializeSessionAsync(AiAgent agent, GaapMessage initMessage)
        {
            // Claude Computer Use doesn't require explicit session initialization
            // The session context is sent with each request via the pre-prompt
            await Task.CompletedTask;
            return true;
        }

        public async IAsyncEnumerable<GaapMessage> StreamOutputAsync(AiAgent agent, GaapMessage request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var client = CreateClient(agent);
            var payload = JsonSerializer.Deserialize<JsonElement>(request.Payload);

            var body = new
            {
                model = agent.ModelName ?? "claude-sonnet-4-6",
                max_tokens = 4096,
                stream = true,
                messages = new[]
                {
                    new { role = "user", content = payload.GetProperty("gherkinContent").GetString() }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var endpoint = string.IsNullOrEmpty(agent.ApiEndpoint)
                ? "https://api.anthropic.com/v1/messages"
                : agent.ApiEndpoint;

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                yield return new GaapMessage
                {
                    MessageType = GaapMessageType.Error,
                    SessionId = request.SessionId,
                    Payload = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                };
                yield break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                yield return new GaapMessage
                {
                    MessageType = GaapMessageType.OutputChunk,
                    SessionId = request.SessionId,
                    Payload = data
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
        {
            // Claude stateless sessions don't require explicit termination
            await Task.CompletedTask;
        }

        private HttpClient CreateClient(AiAgent agent)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", agent.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            client.Timeout = TimeSpan.FromSeconds(agent.TimeoutSeconds > 0 ? agent.TimeoutSeconds : 120);
            return client;
        }
    }
}
