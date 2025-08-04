using Gheetah.Interfaces;
using Gheetah.Models;
using Gheetah.Models.ViewModels.Account;
using Gheetah.Models.ViewModels.Setup;
using Gheetah.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gheetah.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IFileService _fileService;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly IDynamicAuthService _dynamicAuthService;
        private readonly ILogService _logger;
        private readonly ILogger<AccountController> _controllerLogger;

        public AccountController(
            IAuthService authService,
            IFileService fileService,
            IUserService userService,
            IWebHostEnvironment env,
            IDynamicAuthService dynamicAuthService,
            ILogService logger,
            ILogger<AccountController> controllerLogger)
        {
            _authService = authService;
            _fileService = fileService;
            _userService = userService;
            _env = env;
            _dynamicAuthService = dynamicAuthService;
            _logger = logger;
            _controllerLogger = controllerLogger;
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            var provider = await _dynamicAuthService.GetConfiguredProviderAsync();

            if (provider == AuthProviderType.Azure)
            {
                var callbackUrl = Url.Action("Login", "Account", null, Request.Scheme)!;
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync("Azure", new AuthenticationProperties
                {
                    RedirectUri = callbackUrl
                });
                return new EmptyResult();
            }
            else if (provider == AuthProviderType.Google)
            {
                var callbackUrl = Url.Action("Login", "Account", null, Request.Scheme)!;
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync("Google", new AuthenticationProperties
                {
                    RedirectUri = callbackUrl
                });
                return new EmptyResult();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("access_token");

            return RedirectToAction("Login", "Account");
        }

        [HttpGet("account/login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            var provider = await _dynamicAuthService.GetConfiguredProviderAsync();
            ViewBag.Provider = provider.ToString();

            return View();
        }

        [HttpPost("account/login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userService.GetUserByEmail(model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid credentials.");
                return View(model);
            }

            if (user.Roles == null || !user.Roles.Any())
                user.Roles = new List<string> { "Runner", "SSOUser" };
            if (user.Groups == null)
                user.Groups = new List<string>();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("UserType", user.UserType)
            };

            foreach (var role in user.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            foreach (var group in user.Groups)
                claims.Add(new Claim("group", group));

            var token = _authService.GenerateJwtToken(user.Email, user.Roles.FirstOrDefault() ?? "Runner");
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet("account/register")]
        [AllowAnonymous]
        public IActionResult Register(bool first = false)
        {
            if (User.Identity?.IsAuthenticated == true && !first)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpPost("account/register")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _userService.GetUserByEmail(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(model);
            }

            var roles = new List<string> { "Runner", "SSOUser" };
            var groups = new List<string>();

            var superAdmin = await _userService.GetSuperAdmin();
            if (superAdmin == null)
            {
                var perms = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json") ?? new();
                var grps = await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json") ?? new();

                var adminPerm = perms.FirstOrDefault(p => p.Id == "admin-perm");
                if (adminPerm != null) roles.Add(adminPerm.Name);

                var adminGroup = grps.FirstOrDefault(g => g.Id == "admin-grp");
                if (adminGroup != null) groups.Add(adminGroup.Name);
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = model.Email,
                FullName = model.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Roles = roles,
                Groups = groups,
                Status = "Active",
                UserType = "CustomSSO"
            };
            await _userService.CreateOrUpdateUser(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));
            foreach (var group in groups)
                claims.Add(new Claim("group", group));

            var token = _authService.GenerateJwtToken(user.Email, roles.FirstOrDefault() ?? "Runner");
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet("sso/azure")]
        [AllowAnonymous]
        public IActionResult LoginAzure()
        {
            var redirectUrl = Url.Action("AzureCallback", "Account", null, "https");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Azure");
        }

        [HttpGet("sso/azure/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> AzureCallback()
        {
            var result = await HttpContext.AuthenticateAsync("Azure");
            if (!result.Succeeded)
                return BadRequest("Azure SSO failed.");

            var claims = result.Principal.Claims.ToList();
            var email = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email || c.Type == "preferred_username")?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Unknown";

            if (string.IsNullOrEmpty(email))
                return BadRequest("Email not found from Azure.");

            var roles = new List<string> { "Runner", "SSOUser" };
            var groups = new List<string>();

            var superAdmin = await _userService.GetSuperAdmin();
            if (superAdmin == null)
            {
                var perms = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json") ?? new();
                var grps = await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json") ?? new();

                var adminPerm = perms.FirstOrDefault(p => p.Id == "admin-perm");
                if (adminPerm != null)
                {
                    roles.Add(adminPerm.Name);
                    claims.Add(new Claim("Dynamic_admin-perm", "true"));
                }

                var adminGroup = grps.FirstOrDefault(g => g.Id == "admin-grp");
                if (adminGroup != null) groups.Add(adminGroup.Name);
            }

            var user = await _userService.GetUserByEmail(email);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = email,
                    FullName = fullName,
                    Roles = roles,
                    Groups = groups,
                    Status = "Active",
                    UserType = "AzureSSO"
                };
                await _userService.CreateOrUpdateUser(user);
            }

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("UserType", user.UserType)
            };

            foreach (var claim in claims.Where(c => c.Type == "roles" || c.Type == "groups"))
            {
                userClaims.Add(claim);
            }

            foreach (var role in user.Roles)
                userClaims.Add(new Claim(ClaimTypes.Role, role));

            foreach (var group in user.Groups)
                userClaims.Add(new Claim("group", group));

            var token = _authService.GenerateJwtToken(user.Email, roles.FirstOrDefault() ?? "Runner");
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                    RedirectUri = "/Dashboard/Index"
                });

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet("sso/google")]
        [AllowAnonymous]
        public IActionResult LoginGoogle()
        {
            var redirectUrl = Url.Action("GoogleCallback", "Account", null, "https");
            var properties = new AuthenticationProperties 
            {
                RedirectUri = redirectUrl,
                Items = { { "scheme", "Google" } }
            };
            return Challenge(properties, "Google");
        }

        [HttpGet]
        [HttpPost]
        [Route("/sso/google/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback()
        {
            var isAuthenticated = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (isAuthenticated.Succeeded)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var result = await HttpContext.AuthenticateAsync("Google");
            if (!result.Succeeded)
            {
                return RedirectToAction("Login", "Account", new { error = "Google_SSO_failed" });
            }

            var claims = result.Principal.Claims.ToList();
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Unknown";

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account", new { error = "Google_SSO_no_email" });
            }

            var roles = new List<string> { "Runner", "SSOUser" };
            var groups = new List<string>();

            var superAdmin = await _userService.GetSuperAdmin();
            if (superAdmin == null)
            {
                var perms = await _fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json") ?? new();
                var grps = await _fileService.LoadConfigAsync<List<GroupVm>>("groups.json") ?? new();

                var adminPerm = perms.FirstOrDefault(p => p.Id == "admin-perm");
                if (adminPerm != null)
                {
                    roles.Add(adminPerm.Name);
                    claims.Add(new Claim("Dynamic_admin-perm", "true"));
                }

                var adminGroup = grps.FirstOrDefault(g => g.Id == "admin-grp");
                if (adminGroup != null) groups.Add(adminGroup.Name);
            }

            var user = await _userService.GetUserByEmail(email);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = email,
                    FullName = fullName,
                    Roles = roles,
                    Groups = groups,
                    Status = "Active",
                    UserType = "GoogleSSO"
                };
                await _userService.CreateOrUpdateUser(user);
            }

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("UserType", user.UserType)
            };

            foreach (var claim in claims.Where(c => c.Type == "roles" || c.Type == "groups"))
            {
                userClaims.Add(claim);
            }

            foreach (var role in user.Roles)
                userClaims.Add(new Claim(ClaimTypes.Role, role));

            foreach (var group in user.Groups)
                userClaims.Add(new Claim("group", group));

            var token = _authService.GenerateJwtToken(user.Email, roles.FirstOrDefault() ?? "Runner");
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                    RedirectUri = "/Dashboard/Index"
                });

            return RedirectToAction("Index", "Dashboard");
        }

        [AllowAnonymous]
        [HttpGet("Account/AccessDenied")]
        public IActionResult AccessDenied(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        private string GetActiveSsoType()
        {
            var dataPath = Path.Combine(_env.ContentRootPath, "Data");
            if (System.IO.File.Exists(Path.Combine(dataPath, "azure-config.json"))) return "Azure";
            if (System.IO.File.Exists(Path.Combine(dataPath, "google-config.json"))) return "Google";
            return "Custom";
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVm model)
        {
            var user = await _userService.GetUserByEmail(User.Identity.Name);
            if (user == null)
                return NotFound();

            var isValid = await _userService.ValidatePassword(user, model.CurrentPassword);
            if (!isValid)
            {
                ModelState.AddModelError("", "Current password incorrect.");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _userService.CreateOrUpdateUser(user);
            await _logger.LogAsync(user.Email, "ChangePassword", "Password changed.");

            return RedirectToAction("Index", "Dashboard");
        }
        
        [Authorize]
        [HttpGet("account/profile")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userService.GetUserByEmail(User.Identity.Name);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost("account/profile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User model)
        {
            var user = await _userService.GetUserByEmail(User.Identity.Name);
    
            if (string.IsNullOrEmpty(model.NewPassword))
            {
                user.FullName = model.FullName;
                await _userService.CreateOrUpdateUser(user);
                TempData["Success"] = "Full name updated!";
                return RedirectToAction("Profile");
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password wrong!");
                return View(model);
            }

            user.FullName = model.FullName;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _userService.CreateOrUpdateUser(user);
    
            TempData["Success"] = "Profile and password updated!";
            return RedirectToAction("Profile");
        }
    }
}