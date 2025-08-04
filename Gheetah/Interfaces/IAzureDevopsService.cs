using Gheetah.Models.PipelineResultsModel.Azure;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineExecution;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineRun;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineTestRun;

namespace Gheetah.Interfaces
{
    public interface IAzureDevopsService
    {
        Task<List<AzureProject>> GetProjectsAsync(string apiUrl, string accessToken);
        Task<List<AzurePipeline>> GetPipelinesAsync(string apiUrl, string project, string accessToken);
        Task<List<PipelineRun>> GetPipelineRunsAsync(string accessToken, string apiUrl, string project, int pipelineId);
        Task<List<TestResultSummary>> GetTestRunsForPipelineAsync(string accessToken, string apiUrl, string organization, string project, int pipelineId);
        Task<List<PipelineExecutionTestResult>> GetFailedTestResultsAsync(string accessToken, string apiUrl, string project, int runId);

    }
}