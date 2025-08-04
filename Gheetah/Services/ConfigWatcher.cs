using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Gheetah.Services
{
    public class ConfigWatcher : BackgroundService
    {
        private FileSystemWatcher _watcher;
        private readonly IOptionsMonitorCache<OpenIdConnectOptions> _oidcCache;
        private readonly ILogger<ConfigWatcher> _logger;
        private DateTime _lastChange = DateTime.MinValue;
        private const int DebounceTime = 1000;

        public ConfigWatcher(IOptionsMonitorCache<OpenIdConnectOptions> oidcCache, ILogger<ConfigWatcher> logger)
        {
            _oidcCache = oidcCache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            _watcher = new FileSystemWatcher(dataPath)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*-config.json",
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
            _watcher.Dispose();
        }

        private async void OnFileChanged(object source, FileSystemEventArgs e)
        {
            var now = DateTime.Now;
            if ((now - _lastChange).TotalMilliseconds < DebounceTime)
                return;

            _lastChange = now;

            try
            {
                await Task.Delay(500);

                var filename = Path.GetFileName(e.Name).ToLowerInvariant();
                string provider = filename.Contains("azure") ? "Azure" :
                                  filename.Contains("google") ? "Google" : string.Empty;
                if (!string.IsNullOrEmpty(provider))
                {
                    _oidcCache.TryRemove("Azure");
                    _oidcCache.TryRemove("Google");
                    _logger.LogInformation("Options cache cleaned: {Provider}", provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload SSO configuration");
            }
        }
    }
}
