namespace Gheetah.Models.AiModels
{
    public class AgentMemory
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string TargetUrl { get; set; }
        public List<string> TestTypes { get; set; } = new();
        public List<string> GeneratedScenarioTitles { get; set; } = new();
        public List<string> TestedAreas { get; set; } = new();
        public List<string> KnownIssues { get; set; } = new();
        public string LastSessionSummary { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
