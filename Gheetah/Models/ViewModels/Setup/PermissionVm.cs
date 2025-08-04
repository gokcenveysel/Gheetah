using System.ComponentModel.DataAnnotations;

namespace Gheetah.Models.ViewModels.Setup
{
    public class PermissionVm
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }
        public List<string> Actions { get; set; } = new List<string>();
    }
}
