using System.ComponentModel.DataAnnotations;

namespace Gheetah.Models.CICDModel
{
    public class CICDSettingsVm
    {
        public string Id { get; set; }
    
        public string Name { get; set; }
    
        [Url]
        public string ApiUrl { get; set; }

        public string AccessToken { get; set; }
    
        public CICDToolType ToolType { get; set; }
    
        // GitLab specific
        public string GroupId { get; set; }
        public string ProjectId { get; set; }
    
        // Jenkins specific
        public string JobName { get; set; }
        public string Crumb { get; set; }
        public string JenkinsUsername { get; set; }
    
        // Azure specific
        public string Organization { get; set; }
        public string Project { get; set; }
        public string CollectionName { get; set; }
    
        public DateTime CreatedDate { get; set; }
    }
}
