using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using System.Collections.Concurrent;

namespace Gheetah.Services
{
    public class SessionService : ISessionService
    {
        private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();
        private readonly IFileService _fileService;
        private readonly ILogger<SessionService> _logger;
        private const string FileName = "active-sessions.json";

        public SessionService(IFileService fileService, ILogger<SessionService> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        public async Task RestoreFromStorageAsync()
        {
            var stored = await _fileService.LoadConfigAsync<List<SessionInfo>>(FileName) ?? new List<SessionInfo>();
            foreach (var session in stored.Where(s => s.Status == SessionStatus.Active || s.Status == SessionStatus.Paused))
            {
                session.IsRecoverable = true;
                _sessions.TryAdd(session.SessionId, session);
            }
            _logger.LogInformation("Restored {Count} active sessions from storage", _sessions.Count);
        }

        public async Task<SessionInfo> CreateSessionAsync(string projectId, string scenarioId, string agentId, string userId)
        {
            var session = new SessionInfo
            {
                ProjectId = projectId,
                ScenarioId = scenarioId,
                AgentId = agentId,
                UserId = userId,
                Status = SessionStatus.Connecting
            };
            _sessions.TryAdd(session.SessionId, session);
            await PersistAsync();
            return session;
        }

        public async Task UpdateSessionAsync(string sessionId, Action<SessionInfo> update)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                update(session);
                session.LastActivity = DateTime.UtcNow;
                await PersistAsync();
            }
        }

        public Task<SessionInfo> GetSessionAsync(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public async Task TerminateSessionAsync(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Status = SessionStatus.Terminated;
                session.IsRecoverable = false;
                await PersistAsync();
                _sessions.TryRemove(sessionId, out _);
            }
        }

        public Task<List<SessionInfo>> GetActiveSessionsAsync()
        {
            var active = _sessions.Values
                .Where(s => s.Status == SessionStatus.Active || s.Status == SessionStatus.Connecting)
                .ToList();
            return Task.FromResult(active);
        }

        public async Task RecoverSessionAsync(string sessionId, string newConnectionId)
        {
            await UpdateSessionAsync(sessionId, s =>
            {
                s.HubConnectionId = newConnectionId;
                s.Status = SessionStatus.Active;
                s.ReconnectCount++;
            });
        }

        private async Task PersistAsync()
        {
            var sessions = _sessions.Values.ToList();
            await _fileService.SaveConfigAsync(FileName, sessions);
        }
    }
}
