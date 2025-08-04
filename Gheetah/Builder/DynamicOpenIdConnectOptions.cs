using Gheetah.Interfaces;
using Gheetah.Models;
using Gheetah.Models.ViewModels.Setup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Gheetah.Builder
{
    public class DynamicOpenIdConnectOptions : IConfigureNamedOptions<OpenIdConnectOptions>
    {
        private readonly IDynamicAuthService _dynamicAuthService;
        private readonly ILogger<DynamicOpenIdConnectOptions> _logger;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly AuthenticationPropertiesSerializer _serializer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IFileService _fileService;

        public DynamicOpenIdConnectOptions(
            IDynamicAuthService dynamicAuthService,
            ILogger<DynamicOpenIdConnectOptions> logger,
            IDataProtectionProvider dataProtectionProvider,
            AuthenticationPropertiesSerializer serializer,
            ILoggerFactory loggerFactory,
            IFileService fileService)
        {
            _dynamicAuthService = dynamicAuthService;
            _logger = logger;
            _dataProtectionProvider = dataProtectionProvider;
            _serializer = serializer;
            _loggerFactory = loggerFactory;
            _fileService = fileService;
        }

        public void Configure(string? name, OpenIdConnectOptions options)
        {
            ConfigureAsync(name, options).GetAwaiter().GetResult();
        }

        public void Configure(OpenIdConnectOptions options)
        {
            // Can left blank
        }

        private async Task ConfigureAsync(string? name, OpenIdConnectOptions options)
        {
            var configuredProvider = await _dynamicAuthService.GetConfiguredProviderAsync();

            if (configuredProvider == AuthProviderType.None || configuredProvider == AuthProviderType.Custom)
            {
                ApplyDummyConfiguration(options);
                return;
            }

            if (name == "Azure" && configuredProvider == AuthProviderType.Azure)
            {
                var azureConfig = await _dynamicAuthService.GetAzureAsync();
                if (azureConfig == null || !azureConfig.IsValid())
                {
                    throw new InvalidOperationException("Azure SSO integration is missing or invalid!");
                }

                options.Authority = $"{azureConfig.Instance.TrimEnd('/')}/{azureConfig.TenantId}/v2.0";
                options.ClientId = azureConfig.ClientId;
                options.ClientSecret = azureConfig.ClientSecret;
                options.CallbackPath = azureConfig.CallbackPath ?? "/sso/azure/callback";
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.GetClaimsFromUserInfoEndpoint = true;

                options.DataProtectionProvider = _dataProtectionProvider;
                options.StateDataFormat = new CustomPropertiesDataFormat(_serializer, _dataProtectionProvider, _loggerFactory.CreateLogger<CustomPropertiesDataFormat>());
                options.CorrelationCookie.Name = "Gheetah.OAuth.Correlation.Azure";
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(15);
                options.CorrelationCookie.Path = "/";
                options.NonceCookie.Name = "Gheetah.OAuth.Nonce.Azure";
                options.NonceCookie.SameSite = SameSiteMode.None;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.HttpOnly = true;
                options.NonceCookie.MaxAge = TimeSpan.FromMinutes(15);
                options.NonceCookie.Path = "/";
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = async context =>
                    {
                        if (context.Request.Method == "POST" && context.Request.HasFormContentType)
                        {
                            var form = await context.Request.ReadFormAsync();
                            _logger.LogInformation($"Azure: POST body: {string.Join(", ", form.Select(f => $"{f.Key}={f.Value}"))}");
                        }
                    },
                    OnTokenValidated = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.Redirect("/Account/Login?error=Azure_SSO_failed");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            }
            else if (name == "Google" && configuredProvider == AuthProviderType.Google)
            {
                var googleConfig = await _dynamicAuthService.GetGoogleAsync();
                if (googleConfig == null || string.IsNullOrEmpty(googleConfig.ClientId) || string.IsNullOrEmpty(googleConfig.ClientSecret))
                {
                    throw new InvalidOperationException("Google SSO integration is missing or invalid!");
                }

                options.Authority = "https://accounts.google.com";
                options.ClientId = googleConfig.ClientId;
                options.ClientSecret = googleConfig.ClientSecret;
                options.CallbackPath = googleConfig.CallbackPath ?? "/sso/google/callback";
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.ResponseType = "code";
                options.ResponseMode = "form_post";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://accounts.google.com"
                };

                options.DataProtectionProvider = _dataProtectionProvider;
                options.StateDataFormat = new CustomPropertiesDataFormat(_serializer, _dataProtectionProvider, _loggerFactory.CreateLogger<CustomPropertiesDataFormat>());
                options.CorrelationCookie.Name = "Gheetah.OAuth.Correlation.Google";
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(15);
                options.CorrelationCookie.Path = "/";
                options.CorrelationCookie.IsEssential = true; 
                options.NonceCookie.Name = "Gheetah.OAuth.Nonce.Google";
                options.NonceCookie.SameSite = SameSiteMode.None;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.HttpOnly = true;
                options.NonceCookie.MaxAge = TimeSpan.FromMinutes(15);
                options.NonceCookie.Path = "/";
                options.NonceCookie.IsEssential = true;

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        if (string.IsNullOrEmpty(context.ProtocolMessage.State))
                        {
                            context.ProtocolMessage.State = Guid.NewGuid().ToString();
                        }
                        context.ProtocolMessage.Parameters["response_mode"] = "form_post";
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = async context =>
                    {
                        if (context.Request.Method == "POST" && context.Request.HasFormContentType)
                        {
                            var form = await context.Request.ReadFormAsync();
                        }
                        else if (context.Request.Method == "GET")
                        {
                            context.Response.Redirect("/Account/Login");
                            context.HandleResponse();
                        }
                    },
                    OnTokenValidated = async context =>
                    {
                        var claims = context.Principal?.Claims?.Select(c => $"{c.Type}:{c.Value}") ?? Enumerable.Empty<string>();

                        var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                        var fullName = context.Principal?.FindFirst("name")?.Value;
                        if (string.IsNullOrEmpty(fullName))
                        {
                            var givenName = context.Principal?.FindFirst(ClaimTypes.GivenName)?.Value;
                            var surname = context.Principal?.FindFirst(ClaimTypes.Surname)?.Value;
                            fullName = $"{givenName} {surname}".Trim();
                            if (string.IsNullOrEmpty(fullName))
                                fullName = "Unknown User";
                        }

                        if (!string.IsNullOrEmpty(email))
                        {
                            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                            var fileService = context.HttpContext.RequestServices.GetRequiredService<IFileService>();
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<DynamicOpenIdConnectOptions>>();

                            var superAdmin = await userService.GetSuperAdmin();
                            var roles = new List<string> { "Runner", "SSOUser" };
                            var groups = new List<string>();
                            var additionalClaims = new List<Claim>();

                            if (superAdmin == null)
                            {
                                var perms = await fileService.LoadConfigAsync<List<PermissionVm>>("permissions.json") ?? new List<PermissionVm>();
                                var grps = await fileService.LoadConfigAsync<List<GroupVm>>("groups.json") ?? new List<GroupVm>();

                                var adminPerm = perms.FirstOrDefault(p => p.Id == "admin-perm");
                                if (adminPerm != null)
                                {
                                    roles.Add(adminPerm.Name);
                                    additionalClaims.Add(new Claim($"Dynamic_{adminPerm.Id}", "true"));
                                }

                                var adminGroup = grps.FirstOrDefault(g => g.Id == "admin-grp");
                                if (adminGroup != null)
                                {
                                    groups.Add(adminGroup.Name);
                                }
                            }
                            else
                            {
                                roles.Add("SuperAdmin");
                            }

                            var user = await userService.GetUserByEmail(email);
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
                            }
                            else
                            {
                                user.FullName = fullName;
                                user.Roles = roles;
                                user.Groups = groups;
                                user.Status = "Active";
                                user.UserType = "GoogleSSO";
                            }

                            await userService.CreateOrUpdateUser(user);

                            var identity = new ClaimsIdentity(context.Principal!.Identity);
                            identity.AddClaims(additionalClaims);
                            identity.AddClaim(new Claim(ClaimTypes.Name, user.Email));
                            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                            identity.AddClaim(new Claim("UserType", user.UserType));
                            foreach (var role in user.Roles)
                                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                            foreach (var group in user.Groups)
                                identity.AddClaim(new Claim("group", group));

                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                                RedirectUri = "/Dashboard/Index"
                            };

                            await context.HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(identity),
                                authProperties);

                            context.Response.Redirect("/Dashboard/Index");
                            context.HandleResponse();
                        }
                        else
                        {
                            context.Response.Redirect("/Account/Login?error=Google_SSO_no_email");
                            context.HandleResponse();
                        }
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.Redirect("/Account/Login?error=Google_SSO_failed");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            }
            else
            {
                ApplyDummyConfiguration(options);
            }
        }

        private void ApplyDummyConfiguration(OpenIdConnectOptions options)
        {
            options.Authority = "https://dummy.authority/";
            options.ClientId = "dummy-client-id";
            options.ClientSecret = "dummy-client-secret";
            options.CallbackPath = "/dummy-callback";
            _logger.LogWarning("Fake OpenIdConnectOptions values added!");
        }
    }
}