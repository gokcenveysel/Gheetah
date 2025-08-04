namespace Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineTestRun
{
    public class TestResultSummary
    {
        public int TestRunId { get; set; }
        public int TotalCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public string PipelineResult { get; set; }
        public string PipelineBuildNumber {get;set;}
    }

}
