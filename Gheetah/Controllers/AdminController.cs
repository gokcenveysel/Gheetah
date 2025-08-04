using Gheetah.Interfaces;
using Gheetah.Models;
using Gheetah.Models.ViewModels.Setup;
using Gheetah.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
    public class AdminController : Controller
    {
        private readonly IFileService _fileService;
        private readonly IUserService _userService;
        private readonly ILogService _logService;

        public AdminController(IFileService fileService, IUserService userService, ILogService logService)
        {
            _fileService = fileService;
            _userService = userService;
            _logService = logService;
        }

        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> ManageUsers()
        {
            ViewBag.Permissions = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json");
            ViewBag.Groups = await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json");
            return View(await _userService.GetAllUsers());
        }


        [Authorize(Policy = "Dynamic_admin-perm")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _userService.DeleteUser(id);
            await _logService.LogAsync(User.Identity.Name, "DeleteUser", $"User with ID {id} deleted.");
            return RedirectToAction("ManageUsers");
        }


        [Authorize(Policy = "Dynamic_admin-perm")]
        [HttpPost]
        public async Task<IActionResult> UpdateUser(User updatedUser)
        {
            await _userService.UpdateUserDetails(updatedUser);
            await _logService.LogAsync(User.Identity.Name, "UpdateUser", $"User {updatedUser.FullName} updated.");
            return RedirectToAction("ManageUsers");
        }

        [Authorize(Policy = "Dynamic_admin-perm")]
        [HttpPost]
        public async Task<IActionResult> SaveUser(User user)
        {
            await _userService.CreateOrUpdateUser(user);
            await _logService.LogAsync(user.Email, "SaveUser", $"User {user.FullName} saved/updated.");
            return RedirectToAction("ManageUsers");
        }

        [Authorize(Policy = "Dynamic_admin-perm")]
        public async Task<IActionResult> ManagePermissions() =>
            View(await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json"));

        [Authorize(Policy = "Dynamic_admin-perm")]
        [HttpPost]
        public async Task<IActionResult> SavePermissions(List<PermissionVm> permissions)
        {
            await _fileService.SaveConfigAsync("permissions.json", permissions);
            await _userService.SynchronizeUserRoles(permissions);
            return RedirectToAction("ManagePermissions");
        }

        [Authorize(Policy = "Dynamic_admin-perm")]
        public async Task<IActionResult> ManageGroups() =>
            View(await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json"));

        [Authorize(Policy = "Dynamic_admin-perm")]
        [HttpPost]
        public async Task<IActionResult> SaveGroups(List<GroupVm> groups)
        {
            await _fileService.SaveConfigAsync("groups.json", groups);
            await _userService.SynchronizeGroupNamesAsync(groups);
            await _fileService.SyncGroupNamesInPermissions(groups);
            return RedirectToAction("ManageGroups");
        }

        [Authorize(Policy = "Dynamic_admin-perm")]
        public async Task<IActionResult> ViewLogs()
        {
            var logs = await _logService.GetLogsAsync();
            return View(logs);
        }
    }
}
