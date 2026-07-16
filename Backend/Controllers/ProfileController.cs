using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITalentHub.Data;
using AITalentHub.Models;
using System.Linq;

namespace AITalentHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        public class UpdateRecruiterDto
        {
            public string CompanyName { get; set; } = string.Empty;
            public string CompanyDescription { get; set; } = string.Empty;
            public string CompanyWebsite { get; set; } = string.Empty;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User context is not authenticated properly.");
            }
            return userId;
        }

        [HttpGet("recruiter")]
        public async Task<IActionResult> GetRecruiterProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _context.RecruiterProfiles
                                        .Include(r => r.User)
                                        .FirstOrDefaultAsync(r => r.UserId == userId);

            if (profile == null)
            {
                return NotFound("Recruiter profile not found.");
            }

            return Ok(new
            {
                id = profile.Id,
                userId = profile.UserId,
                fullName = profile.User?.FullName,
                email = profile.User?.Email,
                companyName = profile.CompanyName,
                companyDescription = profile.CompanyDescription,
                companyWebsite = profile.CompanyWebsite,
                updatedAt = profile.UpdatedAt
            });
        }

        [HttpPut("recruiter")]
        public async Task<IActionResult> UpdateRecruiterProfile([FromBody] UpdateRecruiterDto dto)
        {
            var userId = GetCurrentUserId();
            var profile = await _context.RecruiterProfiles.FirstOrDefaultAsync(r => r.UserId == userId);

            if (profile == null)
            {
                return NotFound("Recruiter profile not found.");
            }

            profile.CompanyName = dto.CompanyName;
            profile.CompanyDescription = dto.CompanyDescription;
            profile.CompanyWebsite = dto.CompanyWebsite;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Recruiter profile updated successfully.", profile = profile });
        }

        [HttpGet("candidates/search")]
        public async Task<IActionResult> SearchCandidates([FromQuery] string query = "")
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Recruiter" && userRole != "Admin")
            {
                return Forbid();
            }

            var q = query.ToLower();
            
            var candidatesQuery = _context.CandidateProfiles
                .Include(c => c.User)
                .AsQueryable();
                
            if (!string.IsNullOrWhiteSpace(q))
            {
                candidatesQuery = candidatesQuery.Where(c => 
                    (c.User != null && c.User.FullName.ToLower().Contains(q)) ||
                    (c.Skills != null && c.Skills.ToLower().Contains(q)) ||
                    (c.Bio != null && c.Bio.ToLower().Contains(q))
                );
            }

            var candidates = await candidatesQuery.ToListAsync();

            var result = candidates.Select(c => new
            {
                id = c.Id,
                userId = c.UserId,
                fullName = c.User?.FullName,
                email = c.User?.Email,
                bio = c.Bio,
                skills = c.Skills,
                experience = string.IsNullOrEmpty(c.ExperienceJson) ? new object[0] : System.Text.Json.JsonSerializer.Deserialize<object>(c.ExperienceJson),
                education = string.IsNullOrEmpty(c.EducationJson) ? new object[0] : System.Text.Json.JsonSerializer.Deserialize<object>(c.EducationJson),
                projects = string.IsNullOrEmpty(c.ProjectsJson) ? new object[0] : System.Text.Json.JsonSerializer.Deserialize<object>(c.ProjectsJson),
                resumePath = c.ResumePath,
                updatedAt = c.UpdatedAt
            });

            return Ok(result);
        }
    }
}
