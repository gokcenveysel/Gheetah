namespace Gheetah.Models.AiModels
{
    public enum AiScenarioStatus
    {
        Draft,
        Ready,
        Running,
        Passed,
        Failed
    }

    public enum AiScenarioSource
    {
        Manual,
        AiGenerated,
        JiraImported
    }

    public class AiScenario
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProjectId { get; set; }
        public string FeatureName { get; set; }
        public string Title { get; set; }
        public string GherkinContent { get; set; }
        public AiScenarioStatus Status { get; set; } = AiScenarioStatus.Draft;
        public AiScenarioSource Source { get; set; } = AiScenarioSource.Manual;
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public DateTime? LastExecutedDate { get; set; }
        public string LastExecutionId { get; set; }
        public string FilePath { get; set; } // absolute path to .feature file in project folder
    }
}
