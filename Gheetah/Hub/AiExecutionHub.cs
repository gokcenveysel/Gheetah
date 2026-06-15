using Gheetah.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gheetah.Hub
{
    public class AiExecutionHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly ISessionService _sessionService;
        private readonly IExecutionRecoveryService _recoveryService;
        private readonly IAiExecutionService _executionService;
        private readonly ILogger<AiExecutionHub> _logger;

        public AiExecutionHub(
            ISessionService sessionService,
            IExecutionRecoveryService recoveryService,
            IAiExecutionService executionService,
            ILogger<AiExecutionHub> logger)
        {
            _sessionService = sessionService;
            _recoveryService = recoveryService;
            _executionService = executionService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("AI Execution Hub client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("AI Execution Hub client disconnected: {ConnectionId}", Context.ConnectionId);
            // Do NOT mark sessions unrecoverable here — client may reconnect
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeToSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            _logger.LogInformation("Client {ConnectionId} subscribed to session {SessionId}", Context.ConnectionId, sessionId);

            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null) return;

            // Update hub connection id
            await _sessionService.UpdateSessionAsync(sessionId, s => s.HubConnectionId = Context.ConnectionId);

            // Recovery: replay buffered output if reconnecting
            if (session.OutputBuffer.Any() && session.ReconnectCount > 0)
            {
                await _recoveryService.AttemptRecoveryAsync(sessionId, Context.ConnectionId);
            }
        }

        public async Task UnsubscribeFromSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        }

        public async Task CancelExecution(string sessionId)
        {
            await _executionService.CancelExecutionAsync(sessionId);
        }
    }
}
