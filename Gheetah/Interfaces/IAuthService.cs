using Gheetah.Models;
using Gheetah.Models.ViewModels.Account;

namespace Gheetah.Interfaces
{
    public interface IAuthService
    {
        Task<bool> CreateSuperAdmin(User model);
        string GenerateJwtToken(string email, string role);
        Task<AuthResult> RegisterUserAsync(RegisterVm model);
        Task<AuthResult> ValidateCredentials(string email, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);

    }
}
