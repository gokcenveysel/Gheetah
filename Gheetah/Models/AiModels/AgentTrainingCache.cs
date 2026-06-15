namespace Gheetah.Models.AiModels
{
    public class AgentTrainingCache
    {
        public string AgentId { get; set; }
        public string ProjectId { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public List<string> CachedScenarios { get; set; } = new();
        public int TotalTokensEstimate { get; set; }
    }
}
