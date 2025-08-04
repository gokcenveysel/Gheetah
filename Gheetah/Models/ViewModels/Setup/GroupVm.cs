namespace Gheetah.Models.ViewModels.Setup
{
    public class GroupVm
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<PermissionVm> Permissions { get; set; }
    }
}