namespace Gheetah.Models.AiModels
{
    public enum GaapMessageType
    {
        Initialize,
        ScenarioRequest,
        OutputChunk,
        StepStarted,
        StepCompleted,
        StepFailed,
        ScenarioComplete,
        Error,
        Heartbeat,
        EnvironmentQuery,
        ConflictReport,
        Abort
    }

    public class GaapMessage
    {
        public GaapMessageType MessageType { get; set; }
        public string SessionId { get; set; }
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Payload { get; set; }
    }
}
