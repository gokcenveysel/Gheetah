namespace Gheetah.Models.AiModels
{
    public enum AiProjectStatus
    {
        Active,
        Archived
    }

    public class AiProject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public string AiAgentId { get; set; }
        public string PrePromptId { get; set; }
        public string DefaultEnvironmentId { get; set; }
        public AiProjectStatus Status { get; set; } = AiProjectStatus.Active;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string> TestTypes { get; set; } = new();
        public string FolderPath { get; set; } // absolute path inside the configured Project Folder
    }
}
