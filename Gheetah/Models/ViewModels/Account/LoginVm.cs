using System.ComponentModel.DataAnnotations;

namespace Gheetah.Models.ViewModels.Account
{
    public class LoginVm
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
