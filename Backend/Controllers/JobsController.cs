using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITalentHub.Data;
using AITalentHub.Models;
using AITalentHub.Services;

namespace AITalentHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMatchingService _matchingService;

        public JobsController(AppDbContext context, IMatchingService matchingService)
        {
            _context = context;
            _matchingService = matchingService;
        }

        public class JobPostDto
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Requirements { get; set; } = string.Empty; // Semicolon-separated skills
            public string Location { get; set; } = string.Empty;
            public string JobType { get; set; } = "Full-Time";
            public string SalaryRange { get; set; } = string.Empty;
        }

        private async Task<RecruiterProfile?> GetCurrentRecruiterAsync()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int userId)) return null;
            return await _context.RecruiterProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
        }

        private async Task<CandidateProfile?> GetCurrentCandidateAsync()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int userId)) return null;
            return await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllJobs([FromQuery] string? search, [FromQuery] string? location, [FromQuery] string? jobType)
        {
            var query = _context.JobPosts.Include(j => j.RecruiterProfile).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(j => j.Title.ToLower().Contains(s) || j.Description.ToLower().Contains(s) || j.Requirements.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(j => j.Location.ToLower().Contains(location.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(jobType))
            {
                query = query.Where(j => j.JobType.ToLower() == jobType.ToLower());
            }

            var jobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

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

        [HttpGet("my-postings")]
        public async Task<IActionResult> GetMyPostings()
        {
            var recruiter = await GetCurrentRecruiterAsync();
            if (recruiter == null)
            {
                return BadRequest("Only recruiters can view their postings.");
            }

            var jobs = await _context.JobPosts
                                    .Where(j => j.RecruiterProfileId == recruiter.Id)
                                    .OrderByDescending(j => j.CreatedAt)
                                    .Select(j => new
                                    {
                                        j.Id,
                                        j.Title,
                                        j.Description,
                                        j.Requirements,
                                        j.Location,
                                        j.JobType,
                                        j.SalaryRange,
                                        j.CreatedAt,
                                        ApplicantCount = _context.Applications.Count(a => a.JobPostId == j.Id)
                                    })
                                    .ToListAsync();

            return Ok(jobs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobDetails(int id)
        {
            var job = await _context.JobPosts
                                    .Include(j => j.RecruiterProfile)
                                    .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound("Job not found.");
            }

            return Ok(new
            {
                id = job.Id,
                title = job.Title,
                description = job.Description,
                requirements = job.Requirements,
                location = job.Location,
                jobType = job.JobType,
                salaryRange = job.SalaryRange,
                createdAt = job.CreatedAt,
                companyName = job.RecruiterProfile?.CompanyName,
                companyDescription = job.RecruiterProfile?.CompanyDescription,
                companyWebsite = job.RecruiterProfile?.CompanyWebsite
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] JobPostDto dto)
        {
            var recruiter = await GetCurrentRecruiterAsync();
            if (recruiter == null)
            {
                return BadRequest("Only recruiters can post jobs.");
            }

            var job = new JobPost
            {
                RecruiterProfileId = recruiter.Id,
                Title = dto.Title,
                Description = dto.Description,
                Requirements = dto.Requirements,
                Location = dto.Location,
                JobType = dto.JobType,
                SalaryRange = dto.SalaryRange,
                CreatedAt = DateTime.UtcNow
            };

            _context.JobPosts.Add(job);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Job post created successfully.", job = job });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJob(int id, [FromBody] JobPostDto dto)
        {
            var recruiter = await GetCurrentRecruiterAsync();
            if (recruiter == null)
            {
                return BadRequest("Only recruiters can update jobs.");
            }

            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == id && j.RecruiterProfileId == recruiter.Id);
            if (job == null)
            {
                return NotFound("Job post not found or unauthorized.");
            }

            job.Title = dto.Title;
            job.Description = dto.Description;
            job.Requirements = dto.Requirements;
            job.Location = dto.Location;
            job.JobType = dto.JobType;
            job.SalaryRange = dto.SalaryRange;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Job post updated successfully.", job = job });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var recruiter = await GetCurrentRecruiterAsync();
            if (recruiter == null)
            {
                return BadRequest("Only recruiters can delete jobs.");
            }

            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == id && j.RecruiterProfileId == recruiter.Id);
            if (job == null)
            {
                return NotFound("Job post not found or unauthorized.");
            }

            _context.JobPosts.Remove(job);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Job post deleted successfully." });
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations()
        {
            var candidate = await GetCurrentCandidateAsync();
            if (candidate == null)
            {
                return BadRequest("Only candidates can get job recommendations.");
            }

            var jobs = await _context.JobPosts
                                    .Include(j => j.RecruiterProfile)
                                    .ToListAsync();

            var recommendedJobs = new List<object>();

            foreach (var job in jobs)
            {
                var (score, explanation) = _matchingService.CalculateMatch(candidate, job);
                
                // Only recommend jobs with some basic match, or return all with match score
                recommendedJobs.Add(new
                {
                    id = job.Id,
                    title = job.Title,
                    description = job.Description,
                    requirements = job.Requirements,
                    location = job.Location,
                    jobType = job.JobType,
                    salaryRange = job.SalaryRange,
                    companyName = job.RecruiterProfile?.CompanyName ?? "Unknown Company",
                    matchScore = score,
                    matchExplanation = JsonSerializer.Deserialize<object>(explanation)
                });
            }

            // Sort by match score descending
            var sortedRecommendations = recommendedJobs
                                        .Cast<dynamic>()
                                        .OrderByDescending(r => r.matchScore)
                                        .ToList();

            return Ok(sortedRecommendations);
        }
    }
}
