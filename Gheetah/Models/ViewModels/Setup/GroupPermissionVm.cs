namespace Gheetah.Models.ViewModels.Setup
{
    public class GroupPermissionVm
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public List<string> SelectedPermissionIds { get; set; } = new List<string>();
    }
}
