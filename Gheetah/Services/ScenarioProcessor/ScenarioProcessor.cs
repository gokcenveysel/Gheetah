using Gheetah.Interfaces;
using Gheetah.Models.ProcessModel;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.ScenarioModel;
using Hangfire;

namespace Gheetah.Services.ScenarioProcessor
{
    public class ScenarioProcessor : IScenarioProcessor
    {
        private readonly IProcessService _processService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ScenarioProcessor(
            IProcessService processService,
            IBackgroundJobClient backgroundJobClient)
        {
            _processService = processService;
            _backgroundJobClient = backgroundJobClient;
        }
        public string StartScenario(string userId, Project project, RunScenarioRequest request)
        {
            var processId = Guid.NewGuid().ToString();
            var cancellationTokenSource = new CancellationTokenSource();

            var processInfo = new ProcessInfo
            {
                Id = processId,
                UserId = userId,
                CancellationTokenSource = cancellationTokenSource,
                Status = ProcessStatus.Running,
                StartTime = DateTime.UtcNow
            };

            if (!_processService.AddProcess(processInfo))
            {
                throw new Exception("Process creation failed!");
            }

            if (project.LanguageType.Equals("c#", StringComparison.OrdinalIgnoreCase))
            {
                _backgroundJobClient.Enqueue<CSharpScenarioExecutor>(x =>
                    x.ExecuteAsync(processId, project, request, cancellationTokenSource.Token));
            }
            else if (project.LanguageType.Equals("java", StringComparison.OrdinalIgnoreCase))
            {
                _backgroundJobClient.Enqueue<JavaScenarioExecutor>(x =>
                    x.ExecuteAsync(processId, project, request, cancellationTokenSource.Token));
            }

            return processId;
        }

        public string StartAllScenarios(string userId, Project project, RunAllScenariosRequest request)
        {
            var processId = Guid.NewGuid().ToString();
            var cancellationTokenSource = new CancellationTokenSource();

            var processInfo = new ProcessInfo
            {
                Id = processId,
                UserId = userId,
                CancellationTokenSource = cancellationTokenSource,
                Status = ProcessStatus.Running,
                StartTime = DateTime.UtcNow
            };

            _processService.AddProcess(processInfo);

            if (project.LanguageType.Equals("c#", StringComparison.OrdinalIgnoreCase))
            {
                _backgroundJobClient.Enqueue<CSharpAllScenariosExecutor>(x =>
                    x.ExecuteAllAsync(processId, project, request, cancellationTokenSource.Token));
            }
            else if (project.LanguageType.Equals("java", StringComparison.OrdinalIgnoreCase))
            {
                _backgroundJobClient.Enqueue<JavaAllScenariosExecutor>(x =>
                    x.ExecuteAsync(processId, project, request, cancellationTokenSource.Token));
            }

            return processId;
        }
    }
}
