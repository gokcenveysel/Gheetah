using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Gheetah.Services
{
    public class AiExecutionService : IAiExecutionService
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISessionService _sessionService;
        private readonly IHubContext<AiExecutionHub> _hubContext;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<AiExecutionService> _logger;

        public AiExecutionService(
            IServiceScopeFactory scopeFactory,
            ISessionService sessionService,
            IHubContext<AiExecutionHub> hubContext,
            IBackgroundJobClient backgroundJobClient,
            ILogger<AiExecutionService> logger)
        {
            _scopeFactory = scopeFactory;
            _sessionService = sessionService;
            _hubContext = hubContext;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public async Task<string> StartExecutionAsync(string projectId, string scenarioId, string agentId, string userId)
        {
            var session = await _sessionService.CreateSessionAsync(projectId, scenarioId, agentId, userId);
            var cts = new CancellationTokenSource();
            _cancellationTokens.TryAdd(session.SessionId, cts);

            _backgroundJobClient.Enqueue<AiExecutionService>(x =>
                x.ExecuteInBackgroundAsync(session.SessionId, CancellationToken.None));

            return session.SessionId;
        }

        public async Task CancelExecutionAsync(string sessionId)
        {
            if (_cancellationTokens.TryGetValue(sessionId, out var cts))
            {
                cts.Cancel();
                _cancellationTokens.TryRemove(sessionId, out _);
            }

            await _sessionService.UpdateSessionAsync(sessionId, s =>
            {
                s.Status = SessionStatus.Terminated;
                s.IsRecoverable = false;
            });

            await _hubContext.Clients.Group(sessionId)
                .SendAsync("ReceiveAiError", "Execution cancelled by user.");
        }

        public async Task<AiExecutionResult> GetResultAsync(string sessionId)
        {
            using var scope = _scopeFactory.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
            var history = await fileService.LoadConfigAsync<List<AiExecutionResult>>("ai-execution-history.json")
                ?? new List<AiExecutionResult>();
            return history.FirstOrDefault(r => r.SessionId == sessionId);
        }

        public async Task<SessionInfo> GetSessionAsync(string sessionId)
            => await _sessionService.GetSessionAsync(sessionId);

        [AutomaticRetry(Attempts = 0)]
        public async Task ExecuteInBackgroundAsync(string sessionId, CancellationToken ct)
        {
            _cancellationTokens.TryGetValue(sessionId, out var cts);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts?.Token ?? ct);

            await _sessionService.UpdateSessionAsync(sessionId, s => s.Status = SessionStatus.Active);
            await _hubContext.Clients.Group(sessionId).SendAsync("ReceiveSessionStatus", "running");

            using var scope = _scopeFactory.CreateScope();
            var agentService = scope.ServiceProvider.GetRequiredService<IAiAgentService>();
            var scenarioService = scope.ServiceProvider.GetRequiredService<IAiScenarioService>();
            var environmentService = scope.ServiceProvider.GetRequiredService<IEnvironmentService>();
            var prePromptService = scope.ServiceProvider.GetRequiredService<IPrePromptService>();
            var adapterFactory = scope.ServiceProvider.GetRequiredService<AiAdapters.AiAgentAdapterFactory>();
            var gaap = scope.ServiceProvider.GetRequiredService<GheetahAiAgentProtocol>();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found for background execution", sessionId);
                return;
            }

            var agent = await agentService.GetByIdAsync(session.AgentId);
            var scenario = await scenarioService.GetByIdAsync(session.ScenarioId, session.ProjectId);
            var env = await environmentService.GetDefaultForProjectAsync(session.ProjectId);

            if (agent == null || scenario == null)
            {
                await HandleError(sessionId, "Agent or scenario not found.");
                return;
            }

            var transcript = new List<GaapMessage>();

            try
            {
                var prePrompt = await prePromptService.BuildPrePromptAsync(
                    agent.PrePromptId ?? string.Empty,
                    new Dictionary<string, string>
                    {
                        { "ProjectId", session.ProjectId },
                        { "ScenarioTitle", scenario.Title },
                        { "EnvironmentUrl", env?.BaseUrl ?? string.Empty }
                    });

                var initMsg = gaap.BuildInitMessage(scenario, env, prePrompt, sessionId);
                var adapter = adapterFactory.Create(agent.ProviderType.ToString());
                await adapter.InitializeSessionAsync(agent, initMsg);

                var scenarioRequest = gaap.BuildScenarioRequest(scenario, sessionId);

                await foreach (var msg in adapter.StreamOutputAsync(agent, scenarioRequest, linkedCts.Token))
                {
                    if (linkedCts.Token.IsCancellationRequested) break;

                    transcript.Add(msg);

                    if (gaap.IsHeartbeat(msg)) continue;

                    await _sessionService.UpdateSessionAsync(sessionId, s => s.OutputBuffer.Add(msg.Payload));
                    await _hubContext.Clients.Group(sessionId).SendAsync("ReceiveAiOutput", msg.Payload);

                    if (gaap.IsError(msg))
                    {
                        await HandleError(sessionId, msg.Payload);
                        return;
                    }

                    if (gaap.IsComplete(msg)) break;
                }

                var result = gaap.ParseCompletion(sessionId, transcript);
                result.ScenarioId = session.ScenarioId;
                result.ProjectId = session.ProjectId;
                result.AgentId = session.AgentId;
                result.AgentName = agent.Name;
                result.EndTime = DateTime.UtcNow;
                result.TotalDurationMs = (long)(result.EndTime.Value - result.StartTime).TotalMilliseconds;

                if (linkedCts.Token.IsCancellationRequested)
                    result.Status = AiExecutionStatus.Cancelled;

                // Persist result
                var history = await fileService.LoadConfigAsync<List<AiExecutionResult>>("ai-execution-history.json")
                    ?? new List<AiExecutionResult>();
                history.Add(result);
                await fileService.SaveConfigAsync("ai-execution-history.json", history);

                // Update scenario status
                scenario.Status = result.Status == AiExecutionStatus.Passed
                    ? AiScenarioStatus.Passed
                    : AiScenarioStatus.Failed;
                scenario.LastExecutedDate = result.EndTime;
                scenario.LastExecutionId = result.Id;
                var scenarioSvc = scope.ServiceProvider.GetRequiredService<IAiScenarioService>();
                await scenarioSvc.SaveScenarioAsync(scenario);

                await _hubContext.Clients.Group(sessionId)
                    .SendAsync("ReceiveAiCompletion", JsonSerializer.Serialize(result));

                await adapter.TerminateSessionAsync(agent, sessionId);
            }
            catch (OperationCanceledException)
            {
                await HandleError(sessionId, "Execution was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI execution error for session {SessionId}", sessionId);
                await HandleError(sessionId, ex.Message);
            }
            finally
            {
                _cancellationTokens.TryRemove(sessionId, out _);
                await _sessionService.TerminateSessionAsync(sessionId);
            }
        }

        private async Task HandleError(string sessionId, string message)
        {
            await _sessionService.UpdateSessionAsync(sessionId, s =>
            {
                s.Status = SessionStatus.Failed;
                s.IsRecoverable = false;
            });
            await _hubContext.Clients.Group(sessionId).SendAsync("ReceiveAiError", message);
        }
    }
}
