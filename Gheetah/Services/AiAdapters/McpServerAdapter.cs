using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Gheetah.Services.AiAdapters
{
    public class McpServerAdapter : IAiAgentAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<McpServerAdapter> _logger;

        public string ProviderType => "MCP";

        public McpServerAdapter(IHttpClientFactory httpClientFactory, ILogger<McpServerAdapter> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> IsHealthyAsync(AiAgent agent)
        {
            if (string.IsNullOrEmpty(agent.ApiEndpoint)) return false;

            try
            {
                var client = CreateClient(agent);
                var healthUrl = agent.ApiEndpoint.TrimEnd('/') + "/health";
                var response = await client.GetAsync(healthUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP server health check failed for agent {AgentId}", agent.Id);
                return false;
            }
        }

        public async Task<string> GenerateAsync(AiAgent agent, string prompt, CancellationToken ct = default)
        {
            var client = CreateClient(agent);
            var url = agent.ApiEndpoint.TrimEnd('/') + "/generate";
            var body = new { prompt };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }

        public async Task<bool> InitializeSessionAsync(AiAgent agent, GaapMessage initMessage)
        {
            try
            {
                var client = CreateClient(agent);
                var url = agent.ApiEndpoint.TrimEnd('/') + "/session/create";
                var body = new StringContent(JsonSerializer.Serialize(initMessage), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, body);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP session init failed for agent {AgentId}", agent.Id);
                return false;
            }
        }

        public async IAsyncEnumerable<GaapMessage> StreamOutputAsync(AiAgent agent, GaapMessage request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var client = CreateClient(agent);
            var url = agent.ApiEndpoint.TrimEnd('/') + "/execute/stream";
            var body = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = body };
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

                GaapMessage msg;
                try
                {
                    msg = JsonSerializer.Deserialize<GaapMessage>(line,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    msg = new GaapMessage
                    {
                        MessageType = GaapMessageType.OutputChunk,
                        SessionId = request.SessionId,
                        Payload = line
                    };
                }

                if (msg != null) yield return msg;
                if (msg?.MessageType == GaapMessageType.ScenarioComplete) break;
            }
        }

        public async Task TerminateSessionAsync(AiAgent agent, string sessionId)
        {
            try
            {
                var client = CreateClient(agent);
                var url = agent.ApiEndpoint.TrimEnd('/') + $"/session/{sessionId}";
                await client.DeleteAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP session termination failed for session {SessionId}", sessionId);
            }
        }

        private HttpClient CreateClient(AiAgent agent)
        {
            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(agent.ApiKey))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {agent.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(agent.TimeoutSeconds > 0 ? agent.TimeoutSeconds : 120);
            return client;
        }
    }
}
