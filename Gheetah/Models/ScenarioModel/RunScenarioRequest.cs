namespace Gheetah.Models.ScenarioModel
{
    public class RunScenarioRequest
    {
        public Guid projectId { get; set; }
        public string ScenarioTag { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
    }
}
