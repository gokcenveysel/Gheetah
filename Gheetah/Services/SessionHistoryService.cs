using System.Text.Json;
using Gheetah.Interfaces;
using Gheetah.Models.AiModels;

namespace Gheetah.Services
{
    public class SessionHistoryService : ISessionHistoryService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static string SessionDir(string projectFolderPath)
            => Path.Combine(projectFolderPath, "session-history");

        private static string SessionFilePath(string projectFolderPath, string sessionId)
            => Path.Combine(SessionDir(projectFolderPath), $"{sessionId}.json");

        public async Task<List<SessionHistoryEntry>> GetSessionsAsync(string projectId, string projectFolderPath)
        {
            var dir = SessionDir(projectFolderPath);
            if (!Directory.Exists(dir))
                return new List<SessionHistoryEntry>();

            var entries = new List<SessionHistoryEntry>();
            foreach (var file in Directory.GetFiles(dir, "*.json").OrderByDescending(f => f))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var entry = JsonSerializer.Deserialize<SessionHistoryEntry>(json, JsonOpts);
                    if (entry?.ProjectId == projectId)
                        entries.Add(entry);
                }
                catch { /* skip corrupt files */ }
            }
            return entries;
        }

        public async Task<SessionHistoryEntry> GetSessionAsync(string sessionId, string projectFolderPath)
        {
            var path = SessionFilePath(projectFolderPath, sessionId);
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<SessionHistoryEntry>(json, JsonOpts);
        }

        public async Task SaveSessionAsync(SessionHistoryEntry session, string projectFolderPath)
        {
            Directory.CreateDirectory(SessionDir(projectFolderPath));
            await File.WriteAllTextAsync(
                SessionFilePath(projectFolderPath, session.SessionId),
                JsonSerializer.Serialize(session, JsonOpts));
        }
    }
}
