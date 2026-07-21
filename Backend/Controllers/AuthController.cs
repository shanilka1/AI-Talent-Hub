using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AITalentHub.Data;
using AITalentHub.Models;
using AITalentHub.Services;

namespace AITalentHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IAuditLogService _auditLogService;

        public AuthController(AppDbContext context, IConfiguration config, IAuditLogService auditLogService)
        {
            _context = context;
            _config = config;
            _auditLogService = auditLogService;
        }

        public class RegisterDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty; // "Candidate" or "Recruiter"
            public string CompanyName { get; set; } = string.Empty; // Optional for recruiter
        }

        public class LoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Email and Password are required.");
            }

            dto.Role = dto.Role.Trim();
            if (dto.Role != "Candidate" && dto.Role != "Recruiter" && dto.Role != "HiringManager" && dto.Role != "Admin")
            {
                return BadRequest("Role must be 'Candidate', 'Recruiter', 'HiringManager', or 'Admin'.");
            }

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
            {
                return BadRequest("Email is already registered.");
            }

            var user = new User
            {
                Email = dto.Email.ToLower(),
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                FullName = dto.FullName,
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Initialize corresponding profile
            if (user.Role == "Candidate")
            {
                var candidate = new CandidateProfile
                {
                    UserId = user.Id,
                    Bio = "Hello! I am a new candidate.",
                    Skills = "",
                    ExperienceJson = "[]",
                    EducationJson = "[]"
                };
                _context.CandidateProfiles.Add(candidate);
            }
            else if (user.Role == "Recruiter" || user.Role == "HiringManager")
            {
                var recruiter = new RecruiterProfile
                {
                    UserId = user.Id,
                    CompanyName = string.IsNullOrWhiteSpace(dto.CompanyName) ? $"{dto.FullName}'s Company" : dto.CompanyName,
                    CompanyDescription = "We are hiring!",
                    CompanyWebsite = ""
                };
                _context.RecruiterProfiles.Add(recruiter);
            }

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            await _auditLogService.LogAsync(new AuditLog
            {
                UserId = user.Id,
                UserEmail = user.Email,
                UserRole = user.Role,
                Action = "Register",
                Entity = "User",
                EntityId = user.Id.ToString(),
                Details = $"User registered with role {user.Role}."
            });

            return Ok(new { token = token, user = new { id = user.Id, email = user.Email, fullName = user.FullName, role = user.Role } });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            var token = GenerateJwtToken(user);

            await _auditLogService.LogAsync(new AuditLog
            {
                UserId = user.Id,
                UserEmail = user.Email,
                UserRole = user.Role,
                Action = "Login",
                Entity = "User",
                EntityId = user.Id.ToString(),
                Details = "User logged in successfully."
            });

            return Ok(new { token = token, user = new { id = user.Id, email = user.Email, fullName = user.FullName, role = user.Role } });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
