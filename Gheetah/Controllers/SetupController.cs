using Gheetah.Interfaces;
using Gheetah.Models.ViewModels.Setup;
using Microsoft.AspNetCore.Mvc;

namespace Gheetah.Controllers
{
    public class SetupController : Controller
    {
        private readonly ISetupService _setupService;
        private readonly ILogger<SetupController> _logger;
        private readonly IDynamicAuthService _dynamicAuthService;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _env;

        public SetupController(
            ISetupService setupService,
            ILogger<SetupController> logger,
            IDynamicAuthService dynamicAuthService,
            IFileService fileService,
            IWebHostEnvironment env)
        {
            _setupService = setupService;
            _logger = logger;
            _dynamicAuthService = dynamicAuthService;
            _fileService = fileService;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (_fileService.IsSetupComplete())
            {
                _logger.LogInformation("Setup already completed, redirecting to Dashboard.");
                return RedirectToAction("Index", "Dashboard");
            }

            _logger.LogInformation("Setup Index called");
            return View(new SsoSelectionVm
            {
                SelectedSso = null
            });
        }

        [HttpPost]
        public IActionResult SelectSso(SsoSelectionVm model)
        {
            _logger.LogInformation($"SelectSso called, selected SSO: {model.SelectedSso}");
            if (model.SelectedSso == "Azure")
                return RedirectToAction("ConfigureAzure");
            if (model.SelectedSso == "Google")
                return RedirectToAction("ConfigureGoogle");
            return RedirectToAction("ManageGroups");
        }

        public IActionResult ConfigureAzure()
        {
            _logger.LogInformation("ConfigureAzure called");
            return View(new AzureConfigVm());
        }

        public IActionResult ConfigureGoogle()
        {
            _logger.LogInformation("ConfigureGoogle called");
            return View(new GoogleConfigVm());
        }

        [HttpPost]
        public async Task<IActionResult> SaveAzure(AzureConfigVm model)
        {
            try
            {
                await _setupService.SaveAzureConfigAsync(model);
                return RedirectToAction("ManageGroups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Azure config");
                ModelState.AddModelError("", "Azure configuration could not be saved.");
                return View("ConfigureAzure", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveGoogle(GoogleConfigVm model)
        {
            try
            {
                await _setupService.SaveGoogleConfigAsync(model);
                return RedirectToAction("ManageGroups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Google config");
                ModelState.AddModelError("", "Google configuration could not be saved.");
                return View("ConfigureGoogle", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageGroups()
        {
            var groups = await _setupService.GetDefaultGroupsAsync();
            return View(groups);
        }

        [HttpPost]
        public async Task<IActionResult> SaveGroups(List<GroupVm> groups)
        {
            try
            {
                foreach (var group in groups)
                {
                    if (string.IsNullOrWhiteSpace(group.Description))
                        group.Description = "No description provided";
                }
                await _setupService.SaveGroupsAsync(groups);
                return RedirectToAction("ManagePermissions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving groups");
                return RedirectToAction("ManageGroups");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManagePermissions()
        {
            var groups = await _setupService.GetGroupsAsync();
            var permissions = await _setupService.GetAllPermissionsAsync();
            var model = groups.Select(g => new GroupPermissionVm
            {
                GroupId = g.Id,
                GroupName = g.Name,
                SelectedPermissionIds = g.Permissions?.Select(p => p.Id).ToList() ?? new()
            }).ToList();

            var hasSaved = model.Any(m => m.SelectedPermissionIds.Any());
            ViewBag.Permissions = permissions;
            ViewBag.HasSaved = hasSaved;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SavePermissions(List<GroupPermissionVm> model, string action)
        {
            try
            {
                await _setupService.SaveGroupPermissionsAsync(model);

                if (action == "complete")
                {
                    await _setupService.CompleteSetupAsync();
                    _logger.LogInformation("Setup completed via SavePermissions");
                    return RedirectToAction("CompleteSetup");
                }

                return RedirectToAction("ManagePermissions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving permissions");
                return RedirectToAction("ManagePermissions");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CompleteSetup()
        {
            if (_fileService.IsSetupComplete())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                await _setupService.CompleteSetupAsync();
                var providerType = await _dynamicAuthService.GetConfiguredProviderAsync();
                var message = providerType switch
                {
                    AuthProviderType.Azure => "You can now login using Microsoft account.",
                    AuthProviderType.Custom => "You can now create an admin account.",
                    _ => "You can now login."
                };

                return View("CompleteSetup", new CompletionVm
                {
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing setup");
                return RedirectToAction("ManagePermissions");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSsoConfig(SsoConfigVm model)
        {
            try
            {
                if (model.AzureConfig != null && model.AzureConfig.IsValid())
                {
                    await _setupService.SaveAzureConfigAsync(model.AzureConfig);
                }
                else if (model.GoogleConfig != null && model.GoogleConfig.IsValid())
                {
                    await _setupService.SaveGoogleConfigAsync(model.GoogleConfig);
                }
                else
                {
                    _logger.LogWarning("Invalid SSO configuration");
                    ModelState.AddModelError("", "Invalid SSO configuration.");
                    if (model.AzureConfig != null)
                        return RedirectToAction("ConfigureAzure", model);
                    else if (model.GoogleConfig != null)
                        return RedirectToAction("ConfigureGoogle", model);
            
                    return RedirectToAction("SelectSso");
                }

                return RedirectToAction("ManageGroups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save SSO config");
                ModelState.AddModelError("", "SSO configuration could not be saved.");
        
                if (model.AzureConfig != null)
                    return RedirectToAction("ConfigureAzure", model);
                else if (model.GoogleConfig != null)
                    return RedirectToAction("ConfigureGoogle", model);
        
                return RedirectToAction("SelectSso");
            }
        }
    }
}