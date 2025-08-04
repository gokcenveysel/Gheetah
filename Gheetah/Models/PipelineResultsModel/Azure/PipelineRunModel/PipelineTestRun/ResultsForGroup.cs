namespace Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineTestRun
{
    public class ResultsForGroup
    {
        public string GroupByValue { get; set; }
        public ResultsCountByOutcome ResultsCountByOutcome { get; set; }
        public List<TestResultItem> Results { get; set; }
    }
}
