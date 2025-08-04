using System.Text.Json;
using Gheetah.Interfaces;
using Gheetah.Models;

namespace Gheetah.Services
{
    public class LogService : ILogService
    {
        private readonly IFileService _fileService;
        private const string LogFile = "logs.json";

        public LogService(IFileService fileService)
        {
            _fileService = fileService;
        }

        public async Task LogAsync(string userEmail, string action, string description)
        {
            var logs = await _fileService.LoadConfigAsync<List<LogEntry>>(LogFile) ?? new();
            logs.Add(new LogEntry
            {
                UserEmail = userEmail,
                Action = action,
                Description = description
            });

            await _fileService.SaveConfigAsync(LogFile, logs);
        }

        public async Task<List<LogEntry>> GetLogsAsync()
        {
            return await _fileService.LoadConfigAsync<List<LogEntry>>(LogFile) ?? new();
        }
    }

}