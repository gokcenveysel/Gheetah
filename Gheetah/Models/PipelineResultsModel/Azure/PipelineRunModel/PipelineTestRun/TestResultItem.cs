namespace Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineTestRun
{
    public class TestResultItem
    {
        public int Id { get; set; }
        public TestProject Project { get; set; }
        public double DurationInMs { get; set; }
        public string Outcome { get; set; }
        public TestRun TestRun { get; set; }
        public int Priority { get; set; }
        public int TestCaseReferenceId { get; set; }
    }

    public class TestProject
    {
        public string Id { get; set; }
    }

    public class TestRun
    {
        public int Id { get; set; }
    }
}
