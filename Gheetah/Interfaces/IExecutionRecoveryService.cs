using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IExecutionRecoveryService
    {
        Task<List<SessionInfo>> FindRecoverableSessionsAsync();
        Task AttemptRecoveryAsync(string sessionId, string newConnectionId);
        Task MarkUnrecoverableAsync(string sessionId, string reason);
    }
}
