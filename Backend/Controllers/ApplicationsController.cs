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
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Bio { get; set; } = string.Empty;
            public string Skills { get; set; } = string.Empty;
            public string ExperienceJson { get; set; } = "[]";
            public string EducationJson { get; set; } = "[]";
            public string ProjectsJson { get; set; } = "[]";
            public string AttachedResumeFilename { get; set; } = string.Empty;
        }

        public class UpdateInterviewFeedbackDto
        {
            public string Feedback { get; set; } = string.Empty;
            public int CandidateRating { get; set; } = 0; // 1-5 rating score
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

            // Create temporary candidate profile containing application details for scoring
            var tempProfile = new CandidateProfile
            {
                Bio = dto.Bio,
                Skills = dto.Skills,
                ExperienceJson = dto.ExperienceJson,
                EducationJson = dto.EducationJson
            };

            // Perform AI Match calculations based on submitted application details
            var (score, explanationJson) = _matchingService.CalculateMatch(tempProfile, job);

            // Snapshot candidate details at application time
            var snapshot = new
            {
                fullName = string.IsNullOrWhiteSpace(dto.FullName) ? (User.FindFirst("FullName")?.Value ?? "") : dto.FullName,
                email = string.IsNullOrWhiteSpace(dto.Email) ? (User.FindFirst(ClaimTypes.Email)?.Value ?? "") : dto.Email,
                phone = dto.Phone,
                bio = dto.Bio,
                skills = dto.Skills,
                experience = dto.ExperienceJson,
                education = dto.EducationJson,
                projects = dto.ProjectsJson,
                attachedResumeFilename = dto.AttachedResumeFilename
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
                matchExplanation = string.IsNullOrWhiteSpace(a.MatchExplanation) ? null : JsonSerializer.Deserialize<object>(a.MatchExplanation)
            }));
        }

        [HttpGet("job/{jobId}")]
        public async Task<IActionResult> GetJobApplicants(int jobId)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var isHiringManagerOrAdmin = role == "HiringManager" || role == "Admin";

            RecruiterProfile? recruiter = null;
            if (!isHiringManagerOrAdmin)
            {
                recruiter = await GetCurrentRecruiterAsync();
                if (recruiter == null)
                {
                    return BadRequest("Only recruiters can view job applicants.");
                }
            }

            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
            {
                return NotFound("Job post not found.");
            }

            if (!isHiringManagerOrAdmin && job.RecruiterProfileId != recruiter!.Id)
            {
                return Unauthorized("Unauthorized to view applicants for this job.");
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
                matchExplanation = string.IsNullOrWhiteSpace(a.MatchExplanation) ? null : JsonSerializer.Deserialize<object>(a.MatchExplanation),
                resumeSnapshot = string.IsNullOrWhiteSpace(a.ResumeSnapshotJson) ? null : JsonSerializer.Deserialize<object>(a.ResumeSnapshotJson)
            }));
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var isHiringManagerOrAdmin = role == "HiringManager" || role == "Admin";

            RecruiterProfile? recruiter = null;
            if (!isHiringManagerOrAdmin)
            {
                recruiter = await GetCurrentRecruiterAsync();
                if (recruiter == null)
                {
                    return BadRequest("Only recruiters can update application status.");
                }
            }

            var app = await _context.Applications
                                    .Include(a => a.JobPost)
                                    .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null)
            {
                return NotFound("Application not found.");
            }

            if (!isHiringManagerOrAdmin && app.JobPost!.RecruiterProfileId != recruiter!.Id)
            {
                return Unauthorized("Unauthorized to update this application status.");
            }

            var allowedStatuses = new[] { "Applied", "Reviewing", "Shortlisted", "Interviewing", "Offered", "Rejected" };
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
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var isHiringManagerOrAdmin = role == "HiringManager" || role == "Admin";

            RecruiterProfile? recruiter = null;
            if (!isHiringManagerOrAdmin)
            {
                recruiter = await GetCurrentRecruiterAsync();
                if (recruiter == null)
                {
                    return BadRequest("Only recruiters can schedule interviews.");
                }
            }

            var app = await _context.Applications
                                    .Include(a => a.JobPost)
                                    .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null)
            {
                return NotFound("Application not found.");
            }

            if (!isHiringManagerOrAdmin && app.JobPost!.RecruiterProfileId != recruiter!.Id)
            {
                return Unauthorized("Unauthorized to schedule interviews for this application.");
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

            return Ok(new
            {
                message = "Interview scheduled successfully.",
                interview = new
                {
                    id = interview.Id,
                    applicationId = interview.ApplicationId,
                    scheduledTime = interview.ScheduledTime,
                    locationOrLink = interview.LocationOrLink,
                    notes = interview.Notes,
                    createdAt = interview.CreatedAt
                }
            });
        }

        [HttpPut("interview/{interviewId}/feedback")]
        public async Task<IActionResult> UpdateInterviewFeedback(int interviewId, [FromBody] UpdateInterviewFeedbackDto dto)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var isHiringManagerOrAdmin = role == "HiringManager" || role == "Admin";

            RecruiterProfile? recruiter = null;
            if (!isHiringManagerOrAdmin)
            {
                recruiter = await GetCurrentRecruiterAsync();
                if (recruiter == null)
                {
                    return BadRequest("Only recruiters, hiring managers, and admins can update interview feedback.");
                }
            }

            var interview = await _context.Interviews
                                           .Include(i => i.Application)
                                           .ThenInclude(a => a!.JobPost)
                                           .FirstOrDefaultAsync(i => i.Id == interviewId);

            if (interview == null)
            {
                return NotFound("Interview not found.");
            }

            if (!isHiringManagerOrAdmin && interview.Application!.JobPost!.RecruiterProfileId != recruiter!.Id)
            {
                return Unauthorized("Unauthorized to update feedback for this interview.");
            }

            if (dto.CandidateRating < 1 || dto.CandidateRating > 5)
            {
                return BadRequest("Candidate rating must be between 1 and 5.");
            }

            interview.Feedback = dto.Feedback;
            interview.CandidateRating = dto.CandidateRating;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Interview feedback updated successfully.",
                interview = new
                {
                    id = interview.Id,
                    applicationId = interview.ApplicationId,
                    feedback = interview.Feedback,
                    candidateRating = interview.CandidateRating
                }
            });
        }

        [HttpGet("my-interviews")]
        public async Task<IActionResult> GetMyInterviews()
        {
            var recruiter = await GetCurrentRecruiterAsync();
            var candidate = await GetCurrentCandidateAsync();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (recruiter != null || role == "HiringManager" || role == "Admin")
            {
                // Return recruiter/manager interviews
                var query = _context.Interviews
                                               .Include(i => i.Application)
                                               .ThenInclude(a => a!.JobPost)
                                               .Include(i => i.Application)
                                               .ThenInclude(a => a!.CandidateProfile)
                                               .ThenInclude(cp => cp!.User)
                                               .AsQueryable();

                if (role != "HiringManager" && role != "Admin")
                {
                    query = query.Where(i => i.Application!.JobPost!.RecruiterProfileId == recruiter!.Id);
                }

                var interviews = await query.OrderBy(i => i.ScheduledTime).ToListAsync();

                return Ok(interviews.Select(i => new
                {
                    id = i.Id,
                    applicationId = i.ApplicationId,
                    jobTitle = i.Application?.JobPost?.Title,
                    candidateName = i.Application?.CandidateProfile?.User?.FullName,
                    scheduledTime = i.ScheduledTime,
                    locationOrLink = i.LocationOrLink,
                    notes = i.Notes,
                    feedback = i.Feedback,
                    candidateRating = i.CandidateRating
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
                    notes = i.Notes,
                    feedback = i.Feedback,
                    candidateRating = i.CandidateRating
                }));
            }

            return BadRequest("User role not recognized.");
        }
    }
}
