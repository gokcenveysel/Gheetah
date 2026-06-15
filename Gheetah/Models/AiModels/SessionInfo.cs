namespace Gheetah.Models.AiModels
{
    public enum SessionStatus
    {
        Connecting,
        Active,
        Paused,
        Terminated,
        Failed
    }

    public class SessionInfo
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string ProjectId { get; set; }
        public string ScenarioId { get; set; }
        public string AgentId { get; set; }
        public string UserId { get; set; }
        public string HubConnectionId { get; set; }
        public SessionStatus Status { get; set; } = SessionStatus.Connecting;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public List<string> OutputBuffer { get; set; } = new();
        public int ReconnectCount { get; set; }
        public bool IsRecoverable { get; set; } = true;
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
    }
}
