namespace Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineTestRun
{
    public class ResultsCountByOutcome
    {
        public OutcomeDetails Passed { get; set; }
        public OutcomeDetails Failed { get; set; }
        public OutcomeDetails NotExecuted { get; set; }
    }
}
