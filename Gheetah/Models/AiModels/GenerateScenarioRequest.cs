namespace Gheetah.Models.AiModels
{
    public class GenerateScenarioRequest
    {
        public string ProjectId { get; set; }
        public string Topic { get; set; }
        public string AdditionalContext { get; set; }
    }
}
