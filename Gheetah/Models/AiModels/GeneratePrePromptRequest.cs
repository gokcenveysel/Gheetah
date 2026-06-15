namespace Gheetah.Models.AiModels
{
    public class GeneratePrePromptRequest
    {
        public List<string> TestTypes { get; set; } = new();
        public string TargetUrl { get; set; }
        public Dictionary<string, string> Requirements { get; set; } = new();
    }
}
