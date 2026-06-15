namespace Gheetah.Models.AiModels
{
    public class CreateAiProjectRequest
    {
        public AiProject Project { get; set; }
        public Dictionary<string, string> Requirements { get; set; } = new();
    }
}
