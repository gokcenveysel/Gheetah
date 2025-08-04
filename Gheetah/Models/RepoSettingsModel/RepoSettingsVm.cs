namespace Gheetah.Models.RepoSettingsModel
{
    public class RepoSettingsVm
    {
        public string Id { get; set; }
        public string RepoType { get; set; }
        public string DisplayName { get; set; }
        public string AccessToken { get; set; }
        public string Username { get; set; }
        public string DomainName { get; set; }
        public string CollectionName { get; set; }
        public string ProjectName { get; set; }
    }
}