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
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMatchingService _matchingService;

        public ApplicationsController(AppDbContext context, IMatchingService matchingService)
        {
            _context = context;
            _matchingService = matchingService;
        }

        public class ApplyDto
        {
            public int JobPostId { get; set; }
        }

        public class UpdateStatusDto
        {
            public string Status { get; set; } = string.Empty; // "Applied", "Reviewing", "Interviewing", "Offered", "Rejected"
        }

        public class ScheduleInterviewDto
        {
            public DateTime ScheduledTime { get; set; }
            public string LocationOrLink { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
        }

        private async Task<CandidateProfile?> GetCurrentCandidateAsync()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int userId)) return null;
            return await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private async Task<RecruiterProfile?> GetCurrentRecruiterAsync()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int userId)) return null;
            return await _context.RecruiterProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] ApplyDto dto)
        {
            var candidate = await GetCurrentCandidateAsync();
            if (candidate == null)
            {
                return BadRequest("Only candidates can apply for jobs.");
            }

            var job = await _context.JobPosts.FindAsync(dto.JobPostId);
            if (job == null)
            {
                return NotFound("Job post not found.");
            }

            // Check if already applied
            var existingApp = await _context.Applications
                                            .FirstOrDefaultAsync(a => a.JobPostId == dto.JobPostId && a.CandidateProfileId == candidate.Id);
            if (existingApp != null)
            {
                return BadRequest("You have already applied for this job.");
            }

            // Perform AI Match calculations
            var (score, explanationJson) = _matchingService.CalculateMatch(candidate, job);

            // Snapshot candidate details
            var snapshot = new
            {
                fullName = User.FindFirst("FullName")?.Value ?? "",
                bio = candidate.Bio,
                skills = candidate.Skills,
                experience = candidate.ExperienceJson,
                education = candidate.EducationJson
            };

            var app = new Application
            {
                JobPostId = dto.JobPostId,
                CandidateProfileId = candidate.Id,
                AppliedAt = DateTime.UtcNow,
                Status = "Applied",
                MatchScore = score,
                MatchExplanation = explanationJson,
                ResumeSnapshotJson = JsonSerializer.Serialize(snapshot)
            };

            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Application submitted successfully.", applicationId = app.Id, matchScore = score });
        }

        [HttpGet("my-applications")]
        public async Task<IActionResult> GetMyApplications()
        {
            var candidate = await GetCurrentCandidateAsync();
            if (candidate == null)
            {
                return BadRequest("Only candidates can view their applications.");
            }

            var apps = await _context.Applications
                                    .Include(a => a.JobPost)
                                    .ThenInclude(j => j!.RecruiterProfile)
                                    .Where(a => a.CandidateProfileId == candidate.Id)
                                    .OrderByDescending(a => a.AppliedAt)
                                    .ToListAsync();

            return Ok(apps.Select(a => new
            {
                id = a.Id,
                jobId = a.JobPostId,
                jobTitle = a.JobPost?.Title,
                companyName = a.JobPost?.RecruiterProfile?.CompanyName,
                location = a.JobPost?.Location,
                appliedAt = a.AppliedAt,
                status = a.Status,
                matchScore = a.MatchScore,
                matchExplanation = JsonSerializer.Deserialize<object>(a.MatchExplanation)
            }));
        }

        [HttpGet("job/{jobId}")]
        public async Task<IActionResult> GetJobApplicants(int jobId)
        {
            var recruiter = await GetCurrentRecruiterAsync();
            if (recruiter == null)
            {
                return BadRequest("Only recruiters can view job applicants.");
            }

            // Ensure recruiter owns the job
            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == jobId && j.RecruiterProfileId == recruiter.Id);
            if (job == null)
            {
                return NotFound("Job post not found or unauthorized.");
            }

            var apps = await _context.Applications
                                    .Include(a => a.CandidateProfile)
                                    .ThenInclude(cp => cp!.User)
                                    .Where(a => a.JobPostId == jobId)
                                    .OrderByDescending(a => a.MatchScore) // Auto-rank candidates by match score
                                    .ToListAsync();

            return Ok(apps.Select(a => new
            {
                id = a.Id,
                candidateId = a.CandidateProfileId,
                candidateName = a.CandidateProfile?.User?.FullName,
                candidateEmail = a.CandidateProfile?.User?.Email,
                appliedAt = a.AppliedAt,
                status = a.Status,
                matchScore = a.MatchScore,
                matchExplanation = JsonSerializer.Deserialize<object>(a.MatchExplanation),
                resumeSnapshot = JsonSerializer.Deserialize<object>(a.ResumeSnapshotJson)
            }));
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var recruiter = await GetCurrentRecruiterAsync();
            if (recruiter == null)
            {
                return BadRequest("Only recruiters can update application status.");
            }

            var app = await _context.Applications
                                    .Include(a => a.JobPost)
                                    .FirstOrDefaultAsync(a => a.Id == id && a.JobPost!.RecruiterProfileId == recruiter.Id);

            if (app == null)
            {
                return NotFound("Application not found or unauthorized.");
            }

            var allowedStatuses = new[] { "Applied", "Reviewing", "Interviewing", "Offered", "Rejected" };
            if (!allowedStatuses.Contains(dto.Status))
            {
                return BadRequest("Invalid status value.");
            }

            app.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated successfully.", status = app.Status });
        }

        [HttpPost("{id}/schedule-interview")]
        public async Task<IActionResult> ScheduleInterview(int id, [FromBody] ScheduleInterviewDto dto)
        {
            var recruiter = await GetCurrentRecruiterAsync();
            if (recruiter == null)
            {
                return BadRequest("Only recruiters can schedule interviews.");
            }

            var app = await _context.Applications
                                    .Include(a => a.JobPost)
                                    .FirstOrDefaultAsync(a => a.Id == id && a.JobPost!.RecruiterProfileId == recruiter.Id);

            if (app == null)
            {
                return NotFound("Application not found or unauthorized.");
            }

            var interview = new Interview
            {
                ApplicationId = app.Id,
                ScheduledTime = dto.ScheduledTime,
                LocationOrLink = dto.LocationOrLink,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Interviews.Add(interview);
            
            // Auto update status to "Interviewing"
            app.Status = "Interviewing";
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Interview scheduled successfully.", interview = interview });
        }

        [HttpGet("my-interviews")]
        public async Task<IActionResult> GetMyInterviews()
        {
            var recruiter = await GetCurrentRecruiterAsync();
            var candidate = await GetCurrentCandidateAsync();

            if (recruiter != null)
            {
                // Return recruiter interviews
                var interviews = await _context.Interviews
                                               .Include(i => i.Application)
                                               .ThenInclude(a => a!.JobPost)
                                               .Include(i => i.Application)
                                               .ThenInclude(a => a!.CandidateProfile)
                                               .ThenInclude(cp => cp!.User)
                                               .Where(i => i.Application!.JobPost!.RecruiterProfileId == recruiter.Id)
                                               .OrderBy(i => i.ScheduledTime)
                                               .ToListAsync();

                return Ok(interviews.Select(i => new
                {
                    id = i.Id,
                    applicationId = i.ApplicationId,
                    jobTitle = i.Application?.JobPost?.Title,
                    candidateName = i.Application?.CandidateProfile?.User?.FullName,
                    scheduledTime = i.ScheduledTime,
                    locationOrLink = i.LocationOrLink,
                    notes = i.Notes
                }));
            }
            else if (candidate != null)
            {
                // Return candidate interviews
                var interviews = await _context.Interviews
                                               .Include(i => i.Application)
                                               .ThenInclude(a => a!.JobPost)
                                               .ThenInclude(j => j!.RecruiterProfile)
                                               .Where(i => i.Application!.CandidateProfileId == candidate.Id)
                                               .OrderBy(i => i.ScheduledTime)
                                               .ToListAsync();

                return Ok(interviews.Select(i => new
                {
                    id = i.Id,
                    applicationId = i.ApplicationId,
                    jobTitle = i.Application?.JobPost?.Title,
                    companyName = i.Application?.JobPost?.RecruiterProfile?.CompanyName,
                    scheduledTime = i.ScheduledTime,
                    locationOrLink = i.LocationOrLink,
                    notes = i.Notes
                }));
            }

            return BadRequest("User role not recognized.");
        }
    }
}
