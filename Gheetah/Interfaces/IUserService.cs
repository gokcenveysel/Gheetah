using Gheetah.Models;
using Gheetah.Models.ViewModels.Setup;

namespace Gheetah.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByEmail(string email);

        Task<User> GetUserById(string id);

        Task<User> GetSuperAdmin();

        Task<List<User>> GetAllUsers();

        Task CreateOrUpdateUser(User user);

        Task DeleteUser(string id);

        Task<bool> ValidatePassword(User user, string password);

        Task AddRoleToUser(string email, string role);

        Task RemoveRoleFromUser(string email, string role);

        Task<bool> UserHasRole(string email, string role);

        Task<List<User>> GetUsersByRole(string role);

        Task UpdateUserStatus(string email, string status);

        Task UpdateUserDetails(User updatedUser);

        Task<int> GetUserCount();

        Task ClearAllUsers();

        Task SynchronizeUserRoles(List<PermissionVm> permissions);

        Task SynchronizeGroupRoles(List<GroupVm> groups);

        Task SynchronizeGroupNamesAsync(List<GroupVm> updatedGroups);

        Task<List<string>> GetEffectivePermissions(User user);

        Task<bool> VerifyPassword(string userId, string password);
    }
}