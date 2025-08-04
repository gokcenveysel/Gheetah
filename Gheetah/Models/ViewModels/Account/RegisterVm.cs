using System.ComponentModel.DataAnnotations;

namespace Gheetah.Models.ViewModels.Account
{
    public class RegisterVm
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }
    }
}
