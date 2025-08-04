using Gheetah.Interfaces;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineRun;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineTestRun;

namespace Gheetah.Services.Jenkins
{
    public class JenkinsService : IJenkinsService
    {
        public async Task<List<TestResultItem>> GetTestResultsAsync(string project, string pipeline)
        {
            // Jenkins' spesific implementation
            return new List<TestResultItem>();
        }

        public async Task<List<PipelineRun>> GetPipelineRunsAsync(string project, string pipeline)
        {
            // Jenkins' spesific implementation
            return new List<PipelineRun>();
        }
    }
}
