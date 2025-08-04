using Gheetah.Interfaces;
using Gheetah.Models.PipelineResultsModel.Azure;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineExecution;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineRun;
using Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineTestRun;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Gheetah.Services
{
public class AzureDevopsService : IAzureDevopsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureDevopsService> _logger;

        public AzureDevopsService(HttpClient httpClient, ILogger<AzureDevopsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<AzureProject>> GetProjectsAsync(string apiUrl, string accessToken)
        {
            try
            {
                var url = $"{apiUrl.TrimEnd('/')}/_apis/projects?api-version=6.0";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = CreateBasicAuthHeader(accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<AzureProjectResponse>(content);
                return result.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Azure projects");
                throw;
            }
        }

        public async Task<List<AzurePipeline>> GetPipelinesAsync(string apiUrl, string project, string accessToken)
        {
            try
            {
                var url = $"{apiUrl.TrimEnd('/')}/{project}/_apis/pipelines?api-version=6.0-preview.1";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = CreateBasicAuthHeader(accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<AzurePipelineResponse>(content);
                return result.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Azure pipelines");
                throw;
            }
        }

        public async Task<List<PipelineRun>> GetPipelineRunsAsync(string accessToken, string apiUrl, string project, int pipelineId)
        {
            var url = $"{apiUrl.TrimEnd('/')}/{project}/_apis/build/builds?definitions={pipelineId}&$top=10&api-version=7.1";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = CreateBasicAuthHeader(accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PipelineRunResponse>(content);
    
            var filteredRuns = result?.Value?
                .Where(run => run.Result != null && 
                              (run.Result.Equals("failed", StringComparison.OrdinalIgnoreCase) || 
                               run.Result.Equals("succeeded", StringComparison.OrdinalIgnoreCase) ||
                               run.Result.Equals("partiallySucceeded", StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(run => run.Id)
                .Take(5)
                .ToList();

            return filteredRuns ?? new List<PipelineRun>();
        }

        public async Task<List<TestResultSummary>> GetTestRunsForPipelineAsync(string accessToken, string apiUrl, string organization, string project, int pipelineId)
        {
            var pipelineRuns = await GetPipelineRunsAsync(accessToken, apiUrl, project, pipelineId);
            var allTestResultSummaries = new List<TestResultSummary>();

            foreach (var pipelineRun in pipelineRuns)
            {
                var testResultsUrl = $"https://vstmr.dev.azure.com/{organization}/{project}/_apis/testresults/resultdetailsbybuild?buildId={pipelineRun.Id}&api-version=7.1-preview.1";
                var testResultsRequest = new HttpRequestMessage(HttpMethod.Get, testResultsUrl);
                testResultsRequest.Headers.Authorization = CreateBasicAuthHeader(accessToken);

                var response = await _httpClient.SendAsync(testResultsRequest);
                if (!response.IsSuccessStatusCode)
                    continue;

                var content = await response.Content.ReadAsStringAsync();
                var testResultsResponse = JsonConvert.DeserializeObject<TestResultResponse>(content);

                if (testResultsResponse == null || 
                    testResultsResponse.ResultsForGroup == null || 
                    !testResultsResponse.ResultsForGroup.Any() || 
                    testResultsResponse.ResultsForGroup.First().Results == null || 
                    !testResultsResponse.ResultsForGroup.First().Results.Any())
                {
                    continue;
                }

                var resultGroup = testResultsResponse.ResultsForGroup.First();

                var summary = new TestResultSummary
                {
                    TestRunId = resultGroup.Results[0].TestRun.Id,
                    TotalCount = (resultGroup.ResultsCountByOutcome.Passed?.Count ?? 0) 
                                 + (resultGroup.ResultsCountByOutcome.Failed?.Count ?? 0) 
                                 + (resultGroup.ResultsCountByOutcome.NotExecuted?.Count ?? 0),
                    PassedCount = resultGroup.ResultsCountByOutcome.Passed?.Count ?? 0,
                    FailedCount = resultGroup.ResultsCountByOutcome.Failed?.Count ?? 0,
                    SkippedCount = resultGroup.ResultsCountByOutcome.NotExecuted?.Count ?? 0,
                    PipelineResult = pipelineRun.Result,
                    PipelineBuildNumber = pipelineRun.BuildNumber
                };

                allTestResultSummaries.Add(summary);
            }

            return allTestResultSummaries.AsEnumerable().Reverse().ToList();
        }



        public async Task<List<PipelineExecutionTestResult>> GetFailedTestResultsAsync(string accessToken, string apiUrl, string project, int runId)
        {
            var url = $"{apiUrl.TrimEnd('/')}/{project}/_apis/test/Runs/{runId}/results?outcome=Failed&api-version=7.1-preview.3";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = CreateBasicAuthHeader(accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new List<PipelineExecutionTestResult>();

            var content = await response.Content.ReadAsStringAsync();
            var testResults = JsonConvert.DeserializeObject<PipelineExecutionTestResultResponse>(content)?.Value ?? new List<PipelineExecutionTestResult>();

            return testResults
                .Where(tr => tr.Outcome.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }



        private AuthenticationHeaderValue CreateBasicAuthHeader(string accessToken)
        {
            return new AuthenticationHeaderValue("Basic", 
                Convert.ToBase64String(Encoding.ASCII.GetBytes($":{accessToken}")));
        }
    }

}
