using Gheetah.Interfaces;
using Gheetah.Models;
using Gheetah.Models.ViewModels.Account;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Gheetah.Services
{
    public class AuthService : IAuthService
    {
        private readonly IFileService _fileService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _config;

        public AuthService(IFileService fileService, ILogger<AuthService> logger, IConfiguration config)
        {
            _fileService = fileService;
            _logger = logger;
            _config = config;
        }

        public string GenerateJwtToken(string email, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> CreateSuperAdmin(User model)
        {
            try
            {
                model.PasswordHash = HashPassword(model.PasswordHash);
                model.Roles.Add("SuperAdmin");

                var users = await _fileService.LoadConfigAsync<List<User>>("users.json") ?? new List<User>();
                users.Add(model);
                await _fileService.SaveConfigAsync("users.json", users);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin creation failed");
                return false;
            }
        }

        public async Task<AuthResult> RegisterUserAsync(RegisterVm model)
        {
            try
            {
                var users = await _fileService.LoadConfigAsync<List<User>>("users.json") ?? new List<User>();

                if (users.Any(u => u.Email == model.Email))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Email already registered"
                    };
                }

                var newUser = new User
                {
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    Roles = new List<string> { "User" }
                };

                if (await _fileService.ConfigExistsAsync("setup_completed.json") && !users.Any(u => u.Roles.Contains("SuperAdmin")))
                {
                    newUser.Roles.Add("SuperAdmin");
                }

                users.Add(newUser);
                await _fileService.SaveConfigAsync("users.json", users);

                return new AuthResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User registration failed");
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Registration failed. Please try again."
                };
            }
        }

        public async Task<AuthResult> ValidateCredentials(string email, string password)
        {
            var users = await _fileService.LoadConfigAsync<List<User>>("users.json");
            var user = users?.FirstOrDefault(u => u.Email == email && VerifyPassword(password, u.PasswordHash));

            return user != null
                ? new AuthResult { Success = true }
                : new AuthResult { Success = false, ErrorMessage = "Invalid credentials" };
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == storedHash;
        }
    }
}
