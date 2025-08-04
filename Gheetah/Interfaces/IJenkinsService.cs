using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineRun;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineTestRun;

namespace Gheetah.Interfaces
{
    public interface IJenkinsService
    {
        Task<List<TestResultItem>> GetTestResultsAsync(string project, string pipeline);
        Task<List<PipelineRun>> GetPipelineRunsAsync(string project, string pipeline);
    }
}
