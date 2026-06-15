using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Gheetah.Services.AiAdapters
{
    public class OpenAIOperatorAdapter : IAiAgentAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OpenAIOperatorAdapter> _logger;

        public string ProviderType => "OpenAI";

        public OpenAIOperatorAdapter(IHttpClientFactory httpClientFactory, ILogger<OpenAIOperatorAdapter> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> IsHealthyAsync(AiAgent agent)
        {
            try
            {
                var client = CreateClient(agent);
                var response = await client.GetAsync("https://api.openai.com/v1/models");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI health check failed for agent {AgentId}", agent.Id);
                return false;
            }
        }

        public async Task<string> GenerateAsync(AiAgent agent, string prompt, CancellationToken ct = default)
        {
            var client = CreateClient(agent);
            var body = new
            {
                model = agent.ModelName ?? "gpt-4o",
                messages = new[] { new { role = "user", content = prompt } }
            };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            return doc.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }

        public async Task<bool> InitializeSessionAsync(AiAgent agent, GaapMessage initMessage)
        {
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
                model = agent.ModelName ?? "gpt-4o",
                stream = true,
                messages = new[]
                {
                    new { role = "user", content = payload.GetProperty("gherkinContent").GetString() }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = content
            };

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
            => await Task.CompletedTask;

        private HttpClient CreateClient(AiAgent agent)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {agent.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(agent.TimeoutSeconds > 0 ? agent.TimeoutSeconds : 120);
            return client;
        }
    }
}
