namespace Gheetah.Models.AgentModel
{
    public class AgentInfo
    {
        public string AgentId { get; set; }
        public string ConnectionId { get; set; }
        public string OS { get; set; }
        public string EnvironmentName { get; set; }
        public string Status { get; set; }
        public string Availability { get; set; }
        public string AuthToken { get; set; }
        public DateTime LastPingTime { get; set; }
    }
}
