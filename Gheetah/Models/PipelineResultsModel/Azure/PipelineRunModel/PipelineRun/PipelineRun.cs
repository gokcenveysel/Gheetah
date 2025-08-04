namespace Gheetah.Models.PipelineResultsModel.Azure.PipelineRunModel.PipelineRun
{
    public class PipelineRun
    {
        public int Id { get; set; }
        public string BuildNumber { get; set; }
        public string Status { get; set; }
        public string Result { get; set; }
        public DateTime QueueTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public string Url { get; set; }
        public Definition Definition { get; set; }
        public Project Project { get; set; }
        public string SourceBranch { get; set; }
        public string SourceVersion { get; set; }
        public Queue Queue { get; set; }
    }

    public class Definition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class Project
    {
        public string Name { get; set; }
    }

    public class Queue
    {
        public string Name { get; set; }
    }
}
