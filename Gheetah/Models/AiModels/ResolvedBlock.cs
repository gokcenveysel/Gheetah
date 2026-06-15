namespace Gheetah.Models.AiModels
{
    public enum ConflictResolution
    {
        Head,
        Incoming,
        Manual,
        Both
    }

    public class ResolvedBlock
    {
        public int BlockIndex { get; set; }
        public ConflictResolution Resolution { get; set; }
        public string CustomContent { get; set; }
        public string ResolvedBy { get; set; }
        public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;
    }
}
