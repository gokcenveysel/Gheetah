using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IAiExecutionService
    {
        Task<string> StartExecutionAsync(string projectId, string scenarioId, string agentId, string userId);
        Task CancelExecutionAsync(string sessionId);
        Task<AiExecutionResult> GetResultAsync(string sessionId);
        Task<SessionInfo> GetSessionAsync(string sessionId);
        Task ExecuteInBackgroundAsync(string sessionId, CancellationToken ct);
    }
}
