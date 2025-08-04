namespace Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineExecution
{
    public class PipelineExecutionTestResult
    {
        public int Id { get; set; }
        public string Outcome { get; set; }
        public double DurationInMs { get; set; }
        public PipelineExecutionTestCase TestCase { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
    }
}
