using Gheetah.Interfaces;
using Gheetah.Models.CICDModel;
using Gheetah.Models.RepoSettingsModel;
using Gheetah.Models.SiteSettingsModel;
using Gheetah.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Gheetah.Models.MailSettingsModel;

namespace Gheetah.Controllers
{
    [Route("SiteSettings")]
    [Authorize(Policy = "Dynamic_admin-perm")]
    public class SiteSettingsController : Controller
    {
        private readonly IFileService _fileService;
        private readonly ILogService _logService;
        private readonly ICICDSettingsService _cicdSettingsService;
        private readonly IMailService _mailService;

        public SiteSettingsController(
            IFileService fileService, 
            ILogService logservice, 
            ICICDSettingsService cicdSettingsService,
            IMailService mailService)
        {
            _fileService = fileService;
            _logService = logservice;
            _cicdSettingsService = cicdSettingsService;
            _mailService = mailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settingsModel = new SiteSettingsVm
            {
                RepoSettingsList = await _fileService.LoadConfigAsync<List<RepoSettingsVm>>("remote-repos-settings.json") ?? new(),
                CICDSettingsList = await _cicdSettingsService.GetAllAsync() ?? new(),
                MailSettingsList = await _mailService.GetAllMailSettings() ?? new()
            };

            var projectFolder = await _fileService.LoadConfigAsync<string>("project-folder.json") ?? "";

            ViewBag.ProjectFolder = projectFolder;

            return View(settingsModel);
        }

        [HttpPost("SaveRepoSettings")]
        public async Task<IActionResult> SaveRepoSettings(List<RepoSettingsVm> settings)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "System";
                var existingSettings = await _fileService.LoadConfigAsync<List<RepoSettingsVm>>("remote-repos-settings.json") ?? new List<RepoSettingsVm>();
                var addedRepos = new List<string>();

                foreach (var newSetting in settings)
                {
                    bool isDuplicate = existingSettings.Any(x => 
                        x.DomainName == newSetting.DomainName && 
                        x.Username == newSetting.Username &&
                        x.RepoType == newSetting.RepoType);

                    if (!isDuplicate)
                    {
                        newSetting.Id = Guid.NewGuid().ToString();
                        existingSettings.Add(newSetting);
                        addedRepos.Add(newSetting.DisplayName);
                
                        await _logService.LogAsync(userEmail, 
                            "Repository Added", 
                            $"Added {newSetting.RepoType} repo: {newSetting.DisplayName}");
                    }
                }

                await _fileService.SaveConfigAsync("remote-repos-settings.json", existingSettings);

                return Json(new {
                    success = true,
                    message = addedRepos.Any() ? 
                        $"Successfully added {addedRepos.Count} repositories: {string.Join(", ", addedRepos)}" : 
                        "No new repositories were added (possible duplicates)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "An error occurred: " + ex.Message
                });
            }
        }

        [HttpPut("UpdateRepoSettings")]
        public async Task<IActionResult> UpdateRepoSettings(List<RepoSettingsVm> settings)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "System";
                var existingSettings = await _fileService.LoadConfigAsync<List<RepoSettingsVm>>("remote-repos-settings.json") ?? new();
                
                foreach (var updatedSetting in settings)
                {
                    var existing = existingSettings.FirstOrDefault(x => x.Id == updatedSetting.Id);
                    if (existing != null)
                    {
                        existing.DisplayName = updatedSetting.DisplayName;
                        existing.RepoType = updatedSetting.RepoType;
                        existing.AccessToken = updatedSetting.AccessToken;
                        existing.Username = updatedSetting.Username;
                        existing.DomainName = updatedSetting.DomainName;
                        existing.CollectionName = updatedSetting.CollectionName;
                        existing.ProjectName = updatedSetting.ProjectName;
                        
                        await _logService.LogAsync(userEmail, 
                            "Repository Updated", 
                            $"Updated {existing.RepoType} repo: {existing.DisplayName}");
                    }
                }
                
                await _fileService.SaveConfigAsync("remote-repos-settings.json", existingSettings);
                
                return Json(new {
                    success = true,
                    message = "Repository updated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "An error occurred: " + ex.Message
                });
            }
        }

        [HttpDelete("DeleteRepoSettings")]
        public async Task<IActionResult> DeleteRepoSettings(string id)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "System";
                var existingSettings = await _fileService.LoadConfigAsync<List<RepoSettingsVm>>("remote-repos-settings.json") ?? new();
                var repoToDelete = existingSettings.FirstOrDefault(x => x.Id == id);
                
                if (repoToDelete == null)
                {
                    return NotFound(new {
                        success = false,
                        message = "Repository not found"
                    });
                }

                existingSettings.Remove(repoToDelete);
                await _fileService.SaveConfigAsync("remote-repos-settings.json", existingSettings);
                
                await _logService.LogAsync(userEmail, 
                    "Repository Deleted", 
                    $"Deleted {repoToDelete.RepoType} repo: {repoToDelete.DisplayName}");
                
                return Json(new {
                    success = true,
                    message = "Repository deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "An error occurred: " + ex.Message
                });
            }
        }

        [HttpGet("GetRepoById")]
        public async Task<IActionResult> GetRepoById(string id)
        {
            var existingSettings = await _fileService.LoadConfigAsync<List<RepoSettingsVm>>("remote-repos-settings.json") ?? new();
            var repo = existingSettings.FirstOrDefault(x => x.Id == id);
    
            if (repo == null)
            {
                return NotFound();
            }
    
            return Ok(repo);
        }

        [HttpPost("SaveProjectFolder")]
        public async Task<IActionResult> SaveProjectFolder(string folder)
        {
            await _fileService.SaveConfigAsync("project-folder.json", folder);
            return RedirectToAction("Index");
        }
        
        [HttpGet("GetCiCdSettingsById")]
        public async Task<IActionResult> GetCiCdSettingsById(string id)
        {
            var cicdSettings = await _cicdSettingsService.GetByIdAsync(id);
            if (cicdSettings == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                cicdSettings.Id,
                cicdSettings.Name,
                cicdSettings.ApiUrl,
                cicdSettings.AccessToken,
                ToolTypeString = cicdSettings.ToolType.ToString(),
                cicdSettings.GroupId,
                cicdSettings.ProjectId,
                cicdSettings.JobName,
                cicdSettings.Crumb,
                cicdSettings.JenkinsUsername,
                cicdSettings.Organization,
                cicdSettings.Project,
                cicdSettings.CollectionName,
                cicdSettings.CreatedDate
            });
        }

        [HttpPost("CreateCiCdSettings")]
        public async Task<IActionResult> CreateCiCdSettings(List<CICDSettingsVm> settings)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "System";
                var existingSettings = await _cicdSettingsService.GetAllAsync() ?? new List<CICDSettingsVm>();
                var addedConfigs = new List<string>();

                foreach (var newSetting in settings)
                {
                    bool isDuplicate = existingSettings.Any(x => 
                        x.ApiUrl == newSetting.ApiUrl && 
                        x.ToolType == newSetting.ToolType);

                    if (!isDuplicate)
                    {
                        newSetting.Id = Guid.NewGuid().ToString();
                        newSetting.CreatedDate = DateTime.UtcNow;
                        await _cicdSettingsService.SaveAsync(newSetting);
                        addedConfigs.Add(newSetting.Name);
                
                        await _logService.LogAsync(userEmail, 
                            "CI/CD Added", 
                            $"Added {newSetting.ToolType} config: {newSetting.Name}");
                    }
                }

                return Json(new {
                    success = true,
                    message = addedConfigs.Any() ? 
                        $"Successfully added {addedConfigs.Count} configurations: {string.Join(", ", addedConfigs)}" : 
                        "No new configurations were added (possible duplicates)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "An error occurred: " + ex.Message
                });
            }
        }

        [HttpPut("UpdateCiCdSettings")]
        public async Task<IActionResult> UpdateCiCdSettings(string id, CICDSettingsVm model)
        {
            if (id != model.Id)
            {
                return BadRequest("ID mismatch");
            }

            if (ModelState.IsValid)
            {
                await _cicdSettingsService.UpdateAsync(model);
                return Ok(model);
            }

            return BadRequest(ModelState);
        }

        [HttpDelete("DeleteCiCdSettings")]
        public async Task<IActionResult> DeleteCiCdSettings(string id)
        {
            var cicdSettings = await _cicdSettingsService.GetByIdAsync(id);
            if (cicdSettings == null)
            {
                return NotFound();
            }

            await _cicdSettingsService.DeleteAsync(id);
            return Ok(new { success = true, message = "CI/CD settings deleted successfully" });
        }

        [HttpPost("CreateMailSettings")]
        public async Task<IActionResult> CreateMailSettings([FromForm] List<MailSettingsVm> settings)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "System";
                var addedSettings = new List<string>();

                foreach (var newSetting in settings)
                {
                    var createdSetting = await _mailService.CreateMailSettings(newSetting);
                    addedSettings.Add(createdSetting.Name);

                    await _logService.LogAsync(userEmail,
                        "Mail Settings Added",
                        $"Added {createdSetting.Provider} mail settings: {createdSetting.Name}");
                }

                return Json(new
                {
                    success = true,
                    message = addedSettings.Any() ?
                        $"Successfully added {addedSettings.Count} mail settings: {string.Join(", ", addedSettings)}" :
                        "No new mail settings were added"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred: " + ex.Message
                });
            }
        }


        [HttpPut("UpdateMailSettings")]
        public async Task<IActionResult> UpdateMailSettings([FromBody] MailSettingsVm setting)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "System";

                var updatedSetting = await _mailService.UpdateMailSettingsById(setting.Id, setting);

                await _logService.LogAsync(userEmail,
                    "Mail Settings Updated",
                    $"Updated {updatedSetting.Provider} mail settings: {updatedSetting.Name}");

                return Json(new
                {
                    success = true,
                    message = "Mail settings updated successfully",
                    data = updatedSetting
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred: " + ex.Message
                });
            }
        }


        [HttpDelete("DeleteMailSettings")]
        public async Task<IActionResult> DeleteMailSettings(string id)
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "System";
                var settingToDelete = await _mailService.GetMailSettingsById(id);

                if (settingToDelete == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Mail settings not found"
                    });
                }

                var result = await _mailService.DeleteMailSettingsById(id);

                if (result)
                {
                    await _logService.LogAsync(userEmail,
                        "Mail Settings Deleted",
                        $"Deleted {settingToDelete.Provider} mail settings: {settingToDelete.Name}");

                    return Json(new
                    {
                        success = true,
                        message = "Mail settings deleted successfully"
                    });
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to delete mail settings"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred: " + ex.Message
                });
            }
        }


        [HttpGet("GetMailSettingsById")]
        public async Task<IActionResult> GetMailSettingsById(string id)
        {
            var setting = await _mailService.GetMailSettingsById(id);
    
            if (setting == null)
            {
                return NotFound();
            }
    
            return Ok(setting);
        }
    }

}