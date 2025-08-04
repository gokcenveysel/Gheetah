using Gheetah.Models.ProjectModel;
using Gheetah.Models.ScenarioModel;

namespace Gheetah.Interfaces
{
    public interface IScenarioProcessor
    {
        string StartScenario(string userId, Project project, RunScenarioRequest request);
        string StartAllScenarios(string userId, Project project, RunAllScenariosRequest request);
    }
}
