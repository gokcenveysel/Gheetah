using Gheetah.Models;

namespace Gheetah.Services
{
    public interface ILogService
    {
        Task LogAsync(string userEmail, string action, string description);
        Task<List<LogEntry>> GetLogsAsync();
    }
}