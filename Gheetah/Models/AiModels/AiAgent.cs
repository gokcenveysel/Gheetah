namespace Gheetah.Models.AiModels
{
    public enum AiProviderType
    {
        Claude,
        OpenAI,
        Gemini,
        Grok,
        MCP,
        Custom,
        Mock
    }

    public class AiAgent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public AiProviderType ProviderType { get; set; }
        public string ApiEndpoint { get; set; }
        public string ApiKey { get; set; }
        public string ModelName { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsDefault { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public List<string> Capabilities { get; set; } = new();
        public int MaxConcurrentSessions { get; set; } = 1;
        public int TimeoutSeconds { get; set; } = 120;
        public string PrePromptId { get; set; }
        public Dictionary<string, string> ExtraConfig { get; set; } = new();
        public DateTime? LastHealthCheckDate { get; set; }
        public string LastHealthCheckStatus { get; set; }
    }
}
