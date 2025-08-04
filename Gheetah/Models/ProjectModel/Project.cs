namespace Gheetah.Models.ProjectModel
{
    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RepoUrl { get; set; }
        public string LanguageType { get; set; }
        public string TestRunnerAdapter { get; set; }
        public string UserId { get; set; }
        public bool IsBuilt { get; set; }
        public DateTime ClonedDate { get; set; }
        public int FeatureFileCount { get; set; }
        public int ScenarioCount { get; set; }
        public List<ProjectInfo> ProjectInfos { get; set; } = new();
        public bool IsLocked { get; set; }
        public string LockedBy { get; set; }
        public DateTime? LockedAt { get; set; } 
    }
}
