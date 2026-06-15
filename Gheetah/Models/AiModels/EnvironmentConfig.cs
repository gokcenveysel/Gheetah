namespace Gheetah.Models.AiModels
{
    public class EnvironmentConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string ProjectId { get; set; }
        public string BaseUrl { get; set; }
        public string BrowserType { get; set; } = "chromium";
        public Dictionary<string, string> Variables { get; set; } = new();
        public bool IsDefault { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public string AuthType { get; set; }
        public string AuthValue { get; set; }
    }
}
