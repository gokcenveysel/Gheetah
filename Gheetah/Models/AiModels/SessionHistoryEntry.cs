namespace Gheetah.Models.AiModels
{
    public class ConversationTurn
    {
        public string Role { get; set; } // "user" or "assistant"
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SessionHistoryEntry
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string ProjectId { get; set; }
        public string AgentId { get; set; }
        public string InitiatedBy { get; set; }
        public string Purpose { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public List<ConversationTurn> Turns { get; set; } = new();
        public List<string> GeneratedScenarioIds { get; set; } = new();
        public string Outcome { get; set; } // "success" | "cancelled" | "error"
    }
}
