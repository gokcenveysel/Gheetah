namespace Gheetah.Models.AiModels
{
    public enum PrePromptChunkType
    {
        System,
        Context,
        Example,
        Constraint,
        StepDefinition,
        Scenario,
        Config
    }

    public class PrePromptChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PrePromptId { get; set; }
        public int Order { get; set; }
        public string Content { get; set; }
        public int TokenCount { get; set; }
        public PrePromptChunkType ChunkType { get; set; }
        public List<string> Keywords { get; set; } = new();
        public string FileHash { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
