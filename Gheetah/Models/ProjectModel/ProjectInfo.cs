namespace Gheetah.Models.ProjectModel
{
    public class ProjectInfo
    {
        public required string ProjectName { get; set; }
        public required string BuildedTestFileName { get; set; }
        public required string BuildedTestFileFullPath { get; set; }
        public required string BuildInfoFileName { get; set; }
        public required string BuildInfoFileFullPath { get; set; }
        public required string FeatureFilesPath { get; set; }
        public List<FeatureScenarioInfo> Scenarios { get; set; } = new();
    }
}
