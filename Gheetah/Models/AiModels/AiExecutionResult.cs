namespace Gheetah.Models.AiModels
{
    public enum AiExecutionStatus
    {
        Running,
        Passed,
        Failed,
        Cancelled,
        TimedOut,
        AgentError
    }

    public class AiStepResult
    {
        public int Index { get; set; }
        public string Keyword { get; set; }
        public string Text { get; set; }
        public bool Passed { get; set; }
        public long DurationMs { get; set; }
        public string ErrorMessage { get; set; }
        public string Observation { get; set; }
        public string ScreenshotBase64 { get; set; }
    }

    public class AiExecutionResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; }
        public string ScenarioId { get; set; }
        public string ProjectId { get; set; }
        public string AgentId { get; set; }
        public string AgentName { get; set; }
        public string EnvironmentName { get; set; }
        public AiExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public long TotalDurationMs { get; set; }
        public List<string> OutputChunks { get; set; } = new();
        public List<AiStepResult> StepResults { get; set; } = new();
        public string ErrorMessage { get; set; }
        public string HtmlReport { get; set; }
        public string PrePromptUsed { get; set; }
    }
}
