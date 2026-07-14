using System;
using System.Linq;
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
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public class UpdateRoleDto
        {
            public string Role { get; set; } = string.Empty;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalJobs = await _context.JobPosts.CountAsync();
            var totalApplications = await _context.Applications.CountAsync();
            var totalInterviews = await _context.Interviews.CountAsync();

            var rolesBreakdown = await _context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                totalUsers,
                totalJobs,
                totalApplications,
                totalInterviews,
                rolesBreakdown
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? role)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role.ToLower() == role.Trim().ToLower());
            }

            var users = await query
                .Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    fullName = u.FullName,
                    role = u.Role,
                    createdAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var newRole = dto.Role.Trim();
            if (newRole != "Candidate" && newRole != "Recruiter" && newRole != "HiringManager" && newRole != "Admin")
            {
                return BadRequest("Invalid role. Role must be 'Candidate', 'Recruiter', 'HiringManager', or 'Admin'.");
            }

            user.Role = newRole;

            // Make sure profile matches the new role
            if (newRole == "Candidate")
            {
                var candidateExists = await _context.CandidateProfiles.AnyAsync(cp => cp.UserId == user.Id);
                if (!candidateExists)
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
            }
            else if (newRole == "Recruiter" || newRole == "HiringManager")
            {
                var recruiterExists = await _context.RecruiterProfiles.AnyAsync(rp => rp.UserId == user.Id);
                if (!recruiterExists)
                {
                    var recruiter = new RecruiterProfile
                    {
                        UserId = user.Id,
                        CompanyName = $"{user.FullName}'s Company",
                        CompanyDescription = "We are hiring!",
                        CompanyWebsite = ""
                    };
                    _context.RecruiterProfiles.Add(recruiter);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "User role updated successfully.", role = user.Role });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int currentUserId))
            {
                return Unauthorized("Unauthorized.");
            }

            if (currentUserId == id)
            {
                return BadRequest("You cannot delete your own admin account.");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully." });
        }

        [HttpGet("jobs")]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _context.JobPosts
                .Include(j => j.RecruiterProfile)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return Ok(jobs.Select(j => new
            {
                id = j.Id,
                title = j.Title,
                description = j.Description,
                requirements = j.Requirements,
                location = j.Location,
                jobType = j.JobType,
                salaryRange = j.SalaryRange,
                createdAt = j.CreatedAt,
                companyName = j.RecruiterProfile?.CompanyName ?? "Unknown Company"
            }));
        }

        [HttpDelete("jobs/{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job == null)
            {
                return NotFound("Job post not found.");
            }

            _context.JobPosts.Remove(job);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Job post deleted successfully." });
        }
    }
}
