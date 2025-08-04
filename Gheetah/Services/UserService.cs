using Gheetah.Interfaces;
using Gheetah.Models;
using Gheetah.Models.ViewModels.Setup;

namespace Gheetah.Services
{
    public class UserService : IUserService
    {
        private readonly IFileService _fileService;
        private readonly ILogger<UserService> _logger;
        private readonly string _usersFilePath;

        public UserService(IFileService fileService, IWebHostEnvironment env, ILogger<UserService> logger)
        {
            _fileService = fileService;
            _logger = logger;
            _usersFilePath = Path.Combine(env.ContentRootPath, "Data", "users.json");
        }

        public async Task<User> GetUserByEmail(string email)
        {
            try
            {
                var users = await LoadUsersAsync();
                var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetUserByEmail Issue: Email={email}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<User> GetUserById(string id)
        {
            try
            {
                var users = await LoadUsersAsync();
                var user = users.FirstOrDefault(u => u.Id == id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetUserById Issue: Id={id}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<User> GetSuperAdmin()
        {
            try
            {
                var users = await LoadUsersAsync();
                var superAdmin = users.FirstOrDefault(u => u.Roles.Contains("SuperAdmin") || u.Roles.Contains("Admin"));
                return superAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetSuperAdmin Issue: Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<User>> GetAllUsers()
        {
            try
            {
                var users = await LoadUsersAsync();
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetAllUsers Issue: Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task CreateOrUpdateUser(User user)
        {
            try
            {
                var users = await LoadUsersAsync();
        
                var existingUser = users.FirstOrDefault(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
                if (existingUser != null)
                {
                    _logger.LogInformation($"Existing user found, updating: {user.Email}");
                    users.Remove(existingUser);
                }
                else
                {
                    _logger.LogInformation($"Creating a new user: {user.Email}");
                }

                users.Add(user);

                await _fileService.SaveConfigAsync("users.json", users);
            }
            catch (Exception ex)
            {
                _logger.LogError($"CreateOrUpdateUser Issue: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task DeleteUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning($"DeleteUser: Invalid ID, Id={id}");
                    return;
                }

                var users = await LoadUsersAsync();
                var userToDelete = users.FirstOrDefault(u => u.Id == id);

                if (userToDelete != null)
                {
                    users.Remove(userToDelete);
                    await _fileService.SaveConfigAsync("users.json", users);
                }
                else
                {
                    _logger.LogWarning($"DeleteUser: User not found, Id={id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteUser Issue: Id={id}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> ValidatePassword(User user, string password)
        {
            try
            {
                var storedUser = await GetUserByEmail(user.Email);
                if (storedUser == null || string.IsNullOrEmpty(storedUser.PasswordHash))
                {
                    _logger.LogWarning($"ValidatePassword: User or password hash not found, Email={user.Email}");
                    return false;
                }
                bool isValid = BCrypt.Net.BCrypt.Verify(password, storedUser.PasswordHash);
                _logger.LogInformation($"ValidatePassword: Email={user.Email}, Status={isValid}");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ValidatePassword Issue: Email={user.Email}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task AddRoleToUser(string email, string role)
        {
            try
            {
                var user = await GetUserByEmail(email);
                if (user != null && !user.Roles.Contains(role))
                {
                    user.Roles.Add(role);
                    await CreateOrUpdateUser(user);
                }
                else
                {
                    _logger.LogWarning($"AddRoleToUser: User not found or role already exists, Email={email}, Role={role}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AddRoleToUser Issue: Email={email}, Role={role}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task RemoveRoleFromUser(string email, string role)
        {
            try
            {
                var user = await GetUserByEmail(email);
                if (user != null && user.Roles.Contains(role))
                {
                    user.Roles.Remove(role);
                    await CreateOrUpdateUser(user);
                }
                else
                {
                    _logger.LogWarning($"RemoveRoleFromUser: User not found or role already exists, Email={email}, Role={role}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RemoveRoleFromUser Issue: Email={email}, Role={role}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> UserHasRole(string email, string role)
        {
            try
            {
                var user = await GetUserByEmail(email);
                bool hasRole = user != null && user.Roles.Contains(role);
                _logger.LogInformation($"UserHasRole: Email={email}, Role={role}, Status={hasRole}");
                return hasRole;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UserHasRole Issue: Email={email}, Role={role}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<User>> GetUsersByRole(string role)
        {
            try
            {
                var users = await GetAllUsers();
                var filteredUsers = users.Where(u => u.Roles.Contains(role)).ToList();
                return filteredUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetUsersByRole Issue: Role={role}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task UpdateUserStatus(string email, string status)
        {
            try
            {
                var user = await GetUserByEmail(email);
                if (user != null)
                {
                    user.Status = status;
                    await CreateOrUpdateUser(user);
                }
                else
                {
                    _logger.LogWarning($"UpdateUserStatus: User not found, Email={email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateUserStatus Issue: Email={email}, Status={status}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task UpdateUserDetails(User updatedUser)
        {
            try
            {
                var existingUser = await GetUserByEmail(updatedUser.Email);
                if (existingUser == null)
                {
                    _logger.LogWarning($"UpdateUserDetails: User not found, Email={updatedUser.Email}");
                    return;
                }

                existingUser.FullName = updatedUser.FullName ?? existingUser.FullName;
                existingUser.Status = updatedUser.Status ?? existingUser.Status;

                if (!string.IsNullOrEmpty(existingUser.PasswordHash))
                    updatedUser.PasswordHash = existingUser.PasswordHash;

                existingUser.Groups = updatedUser.Groups ?? existingUser.Groups;

                var allGroups = await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json") ?? new();
                var userGroups = allGroups.Where(g => existingUser.Groups.Contains(g.Name)).ToList();
                var newRoles = new HashSet<string>(existingUser.Roles.Where(r => r == "Runner" || r == "SSOUser" || r == "Admin"));

                foreach (var group in userGroups)
                {
                    foreach (var perm in group.Permissions ?? new())
                        newRoles.Add(perm.Name);
                }

                existingUser.Roles = newRoles.ToList();

                await CreateOrUpdateUser(existingUser);
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateUserDetails Issue: Email={updatedUser.Email}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<int> GetUserCount()
        {
            try
            {
                var users = await GetAllUsers();
                return users.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetUserCount Issue: Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task ClearAllUsers()
        {
            try
            {
                await _fileService.SaveConfigAsync("users.json", new List<User>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ClearAllUsers Issue: Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task SynchronizeUserRoles(List<PermissionVm> permissions)
        {
            try
            {
                var users = await GetAllUsers();
                foreach (var user in users)
                {
                    for (int i = 0; i < user.Roles.Count; i++)
                    {
                        var match = permissions.FirstOrDefault(p => p.Name == user.Roles[i] || p.Id == user.Roles[i]);
                        if (match != null)
                        {
                            user.Roles[i] = match.Name;
                        }
                    }
                    await CreateOrUpdateUser(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SynchronizeUserRoles Issue: Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task SynchronizeGroupRoles(List<GroupVm> groups)
        {
            try
            {
                var permissions = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json") ?? new();
                foreach (var group in groups)
                {
                    foreach (var perm in group.Permissions ?? new List<PermissionVm>())
                    {
                        var match = permissions.FirstOrDefault(p => p.Id == perm.Id);
                        if (match != null)
                        {
                            perm.Name = match.Name;
                        }
                    }
                }
                await _fileService.SaveConfigAsync("groups.json", groups);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SynchronizeGroupRoles Issue: Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task SynchronizeGroupNamesAsync(List<GroupVm> updatedGroups)
        {
            try
            {
                var users = await GetAllUsers();
                foreach (var user in users)
                {
                    for (int i = 0; i < user.Roles.Count; i++)
                    {
                        var matchedGroup = updatedGroups.FirstOrDefault(g => g.Id == user.Roles[i] || g.Name == user.Roles[i]);
                        if (matchedGroup != null)
                        {
                            user.Roles[i] = matchedGroup.Name;
                        }
                    }
                    await CreateOrUpdateUser(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SynchronizeGroupNamesAsync Issue: Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<string>> GetEffectivePermissions(User user)
        {
            try
            {
                var permissions = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json") ?? new();
                var groups = await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json") ?? new();

                var userGroups = groups
                    .Where(g => user.Groups.Contains(g.Name))
                    .ToList();

                var permissionIds = userGroups
                    .Where(g => g.Permissions != null)
                    .SelectMany(g => g.Permissions!)
                    .Select(p => p.Id)
                    .Distinct()
                    .ToList();

                var effectiveRoles = permissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .Select(p => p.Name)
                    .ToList();

                if (user.Roles != null)
                    effectiveRoles.AddRange(user.Roles);

                return effectiveRoles.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetEffectivePermissions Issue: Email={user.Email}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> VerifyPassword(string userId, string password)
        {
            try
            {
                var user = await GetUserById(userId);
                if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                {
                    _logger.LogWarning($"VerifyPassword: User or password hash not found, Id={userId}");
                    return false;
                }
                bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError($"VerifyPassword Issue: Id={userId}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }

        private async Task<List<User>> LoadUsersAsync()
        {
            try
            {
                var users = await _fileService.LoadConfigAsync<List<User>>("users.json");
                return users ?? new List<User>();
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning($"LoadUsersAsync: users.json not found, returning new list, File={_usersFilePath}");
                return new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"LoadUsersAsync Issue: File={_usersFilePath}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                throw;
            }
        }
    }
}