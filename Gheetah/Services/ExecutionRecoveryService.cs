using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using Microsoft.AspNetCore.SignalR;

namespace Gheetah.Services
{
    public class ExecutionRecoveryService : IExecutionRecoveryService
    {
        private readonly ISessionService _sessionService;
        private readonly IHubContext<AiExecutionHub> _hubContext;
        private readonly ILogger<ExecutionRecoveryService> _logger;

        public ExecutionRecoveryService(
            ISessionService sessionService,
            IHubContext<AiExecutionHub> hubContext,
            ILogger<ExecutionRecoveryService> logger)
        {
            _sessionService = sessionService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<List<SessionInfo>> FindRecoverableSessionsAsync()
        {
            var active = await _sessionService.GetActiveSessionsAsync();
            return active
                .Where(s => s.IsRecoverable && s.LastActivity < DateTime.UtcNow.AddMinutes(-2))
                .ToList();
        }

        public async Task AttemptRecoveryAsync(string sessionId, string newConnectionId)
        {
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null || !session.IsRecoverable)
            {
                _logger.LogWarning("Cannot recover session {SessionId}: not found or not recoverable", sessionId);
                return;
            }

            await _sessionService.RecoverSessionAsync(sessionId, newConnectionId);

            // Replay buffered output to the reconnected client
            if (session.OutputBuffer.Any())
            {
                foreach (var chunk in session.OutputBuffer)
                {
                    await _hubContext.Clients.Client(newConnectionId)
                        .SendAsync("ReceiveAiOutput", chunk);
                }

                _logger.LogInformation("Replayed {Count} buffered messages to reconnected client {ConnectionId}",
                    session.OutputBuffer.Count, newConnectionId);
            }

            await _hubContext.Clients.Client(newConnectionId)
                .SendAsync("ReceiveSessionStatus", "recovered");
        }

        public async Task MarkUnrecoverableAsync(string sessionId, string reason)
        {
            await _sessionService.UpdateSessionAsync(sessionId, s =>
            {
                s.IsRecoverable = false;
                s.Status = SessionStatus.Failed;
            });
            _logger.LogInformation("Session {SessionId} marked unrecoverable: {Reason}", sessionId, reason);
        }
    }
}
