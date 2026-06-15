using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface ISessionService
    {
        Task<SessionInfo> CreateSessionAsync(string projectId, string scenarioId, string agentId, string userId);
        Task UpdateSessionAsync(string sessionId, Action<SessionInfo> update);
        Task<SessionInfo> GetSessionAsync(string sessionId);
        Task TerminateSessionAsync(string sessionId);
        Task<List<SessionInfo>> GetActiveSessionsAsync();
        Task RecoverSessionAsync(string sessionId, string newConnectionId);
        Task RestoreFromStorageAsync();
    }
}
