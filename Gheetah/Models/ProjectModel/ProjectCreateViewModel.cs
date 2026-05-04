namespace Gheetah.Models.ProjectModel
{
    public class ProjectCreateViewModel
    {
        public string ProjectName { get; set; }
        public string Language { get; set; } // "CSharp" veya "Java"
        public string TestAdapter { get; set; } // "xUnit", "MsUnit", "JUnit", "TestNG"
        public string ProjectType { get; set; } // "Web", "Mobile", "Desktop"
        public List<string> Addons { get; set; } = new List<string>(); // "API", "DB"
        public bool CreateRemoteRepo { get; set; }
        public string CustomSourceUrl { get; set; } // Kullanıcı tarafından sağlanan özel kaynak URL'si
    }
}
