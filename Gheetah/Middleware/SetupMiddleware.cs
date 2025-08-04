using Gheetah.Interfaces;

namespace Gheetah.Middleware
{
    public class SetupMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFileService _fileService;
        private readonly ILogger<SetupMiddleware> _logger;
        private readonly string _dataPath;

        public SetupMiddleware(RequestDelegate next, IFileService fileService, ILogger<SetupMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _fileService = fileService;
            _logger = logger;
            _dataPath = Path.Combine(env.ContentRootPath, "Data");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            if (path.StartsWith("/setup/") || 
                path.StartsWith("/sso/") || 
                path.StartsWith("/css/") || 
                path.StartsWith("/js/") || 
                path.StartsWith("/lib/") || 
                path.EndsWith(".png") || 
                path.EndsWith(".jpg") || 
                path.EndsWith(".ico"))
            {
                await _next(context);
                return;
            }

            if (!_fileService.IsSetupComplete())
            {
                var setupFile = Path.Combine(_dataPath, "setup_completed.json");
                context.Response.Redirect("/setup/index");
                return;
            }

            await _next(context);
        }
    }
}