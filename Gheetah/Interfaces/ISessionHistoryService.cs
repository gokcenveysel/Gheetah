using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface ISessionHistoryService
    {
        Task<List<SessionHistoryEntry>> GetSessionsAsync(string projectId, string projectFolderPath);
        Task<SessionHistoryEntry> GetSessionAsync(string sessionId, string projectFolderPath);
        Task SaveSessionAsync(SessionHistoryEntry session, string projectFolderPath);
    }
}
