using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Gheetah.Helper;

namespace Gheetah.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PasswordHash { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Groups { get; set; } = new();
        public string Status { get; set; }
        public string UserType { get; set; }
        
        [NotMapped]
        [DataType(DataType.Password)]
        [RequiredIf("NewPassword != null", ErrorMessage = "Current password required")]
        public string CurrentPassword { get; set; }
        
        [NotMapped]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; }
        
        [NotMapped]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
