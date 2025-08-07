using Gheetah.Authorization;
using Gheetah.Builder;
using Gheetah.Hangfire;
using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Middleware;
using Gheetah.Services;
using Gheetah.Services.Azure;
using Gheetah.Services.GitHub;
using Gheetah.Services.GitLab;
using Gheetah.Services.Jenkins;
using Gheetah.Services.ScenarioProcessor;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
    });
    serverOptions.Limits.MinRequestBodyDataRate = null;
    serverOptions.ListenAnyIP(7169, listenOptions =>
    {
        listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50 MB
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<AuthenticationPropertiesSerializer>();

builder.Services.AddSingleton<IProcessService>(sp =>
{
    var hubContext = sp.GetRequiredService<IHubContext<GheetahHub>>();
    var backgroundJobClient = sp.GetRequiredService<IBackgroundJobClient>();
    var testResultProcessor = sp.GetRequiredService<ITestResultProcessor>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    Console.WriteLine($"IProcessService factory: IHubContext={hubContext != null}, IBackgroundJobClient={backgroundJobClient != null}, ITestResultProcessor={testResultProcessor != null}, IHttpContextAccessor={httpContextAccessor != null}");
    return new ProcessService(httpContextAccessor, hubContext, backgroundJobClient, testResultProcessor);
});
builder.Services.AddSingleton<ITestResultProcessor, TestResultProcessor>();
builder.Services.AddSingleton<IFileService, FileService>();
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddSingleton<IDynamicAuthService, DynamicAuthService>();
builder.Services.AddScoped<ISetupService>(sp =>
{
    var fileService = sp.GetRequiredService<IFileService>();
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var userService = sp.GetRequiredService<IUserService>();
    var dynamicAuthService = sp.GetRequiredService<IDynamicAuthService>();
    return new SetupService(fileService, config, env, userService, dynamicAuthService);
});
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IGitRepoService, AzureRepoService>();
builder.Services.AddScoped<IGitRepoService, GitHubRepoService>();
builder.Services.AddScoped<IGitRepoService, GitLabRepoService>();
builder.Services.AddScoped<ICICDSettingsService, CICDSettingsService>();
builder.Services.AddScoped<IAzureDevopsService, AzureDevopsService>();
builder.Services.AddScoped<IJenkinsService, JenkinsService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IScenarioProcessor, ScenarioProcessor>();
builder.Services.AddScoped<CSharpScenarioExecutor>();
builder.Services.AddScoped<AgentPingService>();
builder.Services.AddScoped<JavaScenarioExecutor>();
builder.Services.AddScoped<CSharpAllScenariosExecutor>();
builder.Services.AddScoped<JavaAllScenariosExecutor>();
builder.Services.AddScoped<AgentManager>();
builder.Services.AddScoped<JavaProjectConfigurator>();

builder.Services.AddSignalR();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Dynamic_admin-lead-policy", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") || context.User.IsInRole("Lead")));
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.Secure = CookieSecurePolicy.Always;
    options.HttpOnly = HttpOnlyPolicy.Always;
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Keys")))
    .SetApplicationName("Gheetah")
    .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
    {
        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
    })
    .ProtectKeysWithDpapi();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "Gheetah.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/Account/AccessDenied";
})
.AddOpenIdConnect("Azure", options => { }) 
.AddOpenIdConnect("Google", options => { })
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
    };
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>>(sp =>
{
    var dynamicAuthService = sp.GetRequiredService<IDynamicAuthService>();
    var logger = sp.GetRequiredService<ILogger<DynamicOpenIdConnectOptions>>();
    var dataProtectionProvider = sp.GetRequiredService<IDataProtectionProvider>();
    var serializer = sp.GetRequiredService<AuthenticationPropertiesSerializer>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var fileService = sp.GetRequiredService<IFileService>();
    return new DynamicOpenIdConnectOptions(dynamicAuthService, logger, dataProtectionProvider, serializer, loggerFactory, fileService);
});
builder.Services.AddSingleton<IOptionsMonitorCache<OpenIdConnectOptions>, OptionsCache<OpenIdConnectOptions>>();
builder.Services.AddHostedService<ConfigWatcher>();

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

var app = builder.Build();

try
{
    app.Logger.LogInformation("Gheetah startup process has begun.");
    var fileService = app.Services.GetRequiredService<IFileService>();
    bool setupComplete = fileService.IsSetupComplete();
    app.Logger.LogInformation($"Setup status: {setupComplete}");

    app.UseHsts();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCookiePolicy();
    app.UseMiddleware<SetupMiddleware>(app.Services.GetRequiredService<IFileService>(), app.Services.GetRequiredService<ILogger<SetupMiddleware>>(), app.Services.GetRequiredService<IWebHostEnvironment>());
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/sso/google"))
        {
            app.Logger.LogInformation($"Google SSO request: {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
            context.Response.OnStarting(() =>
            {
                app.Logger.LogInformation($"Response titles: {string.Join(", ", context.Response.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
                return Task.CompletedTask;
            });
        }
        await next();
        if (context.Request.Path.StartsWithSegments("/sso/google/callback"))
        {
            app.Logger.LogInformation($"Cookies received upon return: {string.Join(", ", context.Request.Cookies.Keys)}");
        }
    });
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "Gheetah Jobs Dashboard",
        Authorization = new[] { new HangfireAuthorizationFilter() },
        IsReadOnlyFunc = context => false,
        DisplayStorageConnectionString = false
    });

    app.MapHub<GheetahHub>("/gheetahHub");
    app.MapControllers();
    app.MapGet("/", context =>
    {
        context.Response.Redirect("/account/login");
        return Task.CompletedTask;
    });
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}");
    app.Logger.LogInformation("Gheetah starting...");
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogError($"An issue happen when Gheetah start: {ex.Message}\nStackTrace: {ex.StackTrace}");
    throw;
}