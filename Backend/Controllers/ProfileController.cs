using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITalentHub.Data;
using AITalentHub.Models;

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
    }
}
