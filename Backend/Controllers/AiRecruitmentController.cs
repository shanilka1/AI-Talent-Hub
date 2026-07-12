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
    public class AiRecruitmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly IMatchingService _matchingService;

        public AiRecruitmentController(AppDbContext context, IGeminiService geminiService, IMatchingService matchingService)
        {
            _context = context;
            _geminiService = geminiService;
            _matchingService = matchingService;
        }

        public class ChatMessageDto
        {
            public string Message { get; set; } = string.Empty;
        }

        private async Task<CandidateProfile?> GetCurrentCandidateAsync()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int userId)) return null;
            return await _context.CandidateProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.UserId == userId);
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatMessageDto dto)
        {
            var candidate = await GetCurrentCandidateAsync();
            if (candidate == null)
            {
                return BadRequest("Only candidates can access conversational onboarding.");
            }

            // Get current chat state
            var onboardingState = candidate.OnboardingStateJson;
            if (string.IsNullOrWhiteSpace(onboardingState) || onboardingState == "{}")
            {
                // Init onboardingState
                onboardingState = JsonSerializer.Serialize(new
                {
                    currentStep = 0,
                    isComplete = false,
                    extractedDetails = new Dictionary<string, object>
                    {
                        { "fullName", candidate.User?.FullName ?? "" },
                        { "email", candidate.User?.Email ?? "" },
                        { "phone", candidate.Phone },
                        { "address", candidate.Address },
                        { "dateOfBirth", candidate.DateOfBirth },
                        { "careerObjective", candidate.CareerObjective },
                        { "skills", candidate.Skills },
                        { "languages", candidate.Languages },
                        { "preferredJobCategory", candidate.PreferredJobCategory },
                        { "preferredSalary", candidate.PreferredSalary },
                        { "preferredLocation", candidate.PreferredLocation },
                        { "linkedInUrl", candidate.LinkedInUrl },
                        { "gitHubUrl", candidate.GitHubUrl },
                        { "education", JsonSerializer.Deserialize<object>(candidate.EducationJson) ?? new List<object>() },
                        { "experience", JsonSerializer.Deserialize<object>(candidate.ExperienceJson) ?? new List<object>() },
                        { "certifications", JsonSerializer.Deserialize<object>(candidate.CertificationsJson) ?? new List<object>() },
                        { "projects", JsonSerializer.Deserialize<object>(candidate.ProjectsJson) ?? new List<object>() },
                        { "achievements", JsonSerializer.Deserialize<object>(candidate.AchievementsJson) ?? new List<string>() },
                        { "references", JsonSerializer.Deserialize<object>(candidate.ReferencesJson) ?? new List<object>() }
                    }
                });
            }

            // Call Gemini/Fallback service to process message
            var (nextPrompt, extractedDetailsJson, isComplete) = await _geminiService.OnboardChatAsync(onboardingState, dto.Message);

            // Update candidate profile with extracted details
            try
            {
                using var doc = JsonDocument.Parse(extractedDetailsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("phone", out var p)) candidate.Phone = p.GetString() ?? "";
                if (root.TryGetProperty("address", out var a)) candidate.Address = a.GetString() ?? "";
                if (root.TryGetProperty("dateOfBirth", out var dob)) candidate.DateOfBirth = dob.GetString() ?? "";
                if (root.TryGetProperty("careerObjective", out var co)) candidate.CareerObjective = co.GetString() ?? "";
                if (root.TryGetProperty("bio", out var bioVal)) candidate.Bio = bioVal.GetString() ?? candidate.Bio;
                if (root.TryGetProperty("skills", out var sk)) candidate.Skills = sk.GetString() ?? "";
                if (root.TryGetProperty("languages", out var lang)) candidate.Languages = lang.GetString() ?? "";
                if (root.TryGetProperty("preferredJobCategory", out var pjc)) candidate.PreferredJobCategory = pjc.GetString() ?? "";
                if (root.TryGetProperty("preferredSalary", out var ps)) candidate.PreferredSalary = ps.GetString() ?? "";
                if (root.TryGetProperty("preferredLocation", out var pl)) candidate.PreferredLocation = pl.GetString() ?? "";
                if (root.TryGetProperty("linkedInUrl", out var li)) candidate.LinkedInUrl = li.GetString() ?? "";
                if (root.TryGetProperty("gitHubUrl", out var gh)) candidate.GitHubUrl = gh.GetString() ?? "";

                if (root.TryGetProperty("education", out var edu)) candidate.EducationJson = edu.GetRawText();
                if (root.TryGetProperty("experience", out var exp)) candidate.ExperienceJson = exp.GetRawText();
                if (root.TryGetProperty("certifications", out var certs)) candidate.CertificationsJson = certs.GetRawText();
                if (root.TryGetProperty("projects", out var proj)) candidate.ProjectsJson = proj.GetRawText();
                if (root.TryGetProperty("achievements", out var ach)) candidate.AchievementsJson = ach.GetRawText();
                if (root.TryGetProperty("references", out var refs)) candidate.ReferencesJson = refs.GetRawText();

                // If bio is still empty, populate from CareerObjective
                if (string.IsNullOrWhiteSpace(candidate.Bio) && !string.IsNullOrWhiteSpace(candidate.CareerObjective))
                {
                    candidate.Bio = candidate.CareerObjective;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing extracted fields: {ex.Message}");
            }

            // Save state back to DB
            int stepNum = 1;
            try
            {
                using var stateDoc = JsonDocument.Parse(onboardingState);
                if (stateDoc.RootElement.TryGetProperty("currentStep", out var stepVal))
                {
                    stepNum = stepVal.GetInt32();
                }
            }
            catch { }

            var newStateObj = new
            {
                currentStep = isComplete ? 18 : stepNum + 1,
                isComplete = isComplete,
                extractedDetails = JsonSerializer.Deserialize<object>(extractedDetailsJson)
            };
            candidate.OnboardingStateJson = JsonSerializer.Serialize(newStateObj);
            candidate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var autoAppliedJobsList = new List<object>();

            // If onboarding is complete, run CV analysis and automatic job match application/interview scheduling
            if (isComplete)
            {
                // 1. Run AI analysis
                var analysisJson = await _geminiService.AnalyzeResumeAsync(candidate, candidate.User?.FullName ?? "", candidate.User?.Email ?? "");
                candidate.AiAnalysisReportJson = analysisJson;
                await _context.SaveChangesAsync();

                // 2. Perform Automatic Matching & Interview Scheduling for jobs matching >= 85%
                var allJobs = await _context.JobPosts.Include(j => j.RecruiterProfile).ToListAsync();
                foreach (var job in allJobs)
                {
                    var (score, explanation) = _matchingService.CalculateMatch(candidate, job);
                    if (score >= 85.0)
                    {
                        // Check if already applied
                        var alreadyApplied = await _context.Applications.AnyAsync(a => a.JobPostId == job.Id && a.CandidateProfileId == candidate.Id);
                        if (!alreadyApplied)
                        {
                            // Submit application snapshot
                            var snapshotObj = new
                            {
                                fullName = candidate.User?.FullName ?? "",
                                email = candidate.User?.Email ?? "",
                                phone = candidate.Phone,
                                bio = candidate.Bio,
                                skills = candidate.Skills,
                                experience = candidate.ExperienceJson,
                                education = candidate.EducationJson,
                                certifications = candidate.CertificationsJson,
                                projects = candidate.ProjectsJson
                            };

                            var application = new Application
                            {
                                JobPostId = job.Id,
                                CandidateProfileId = candidate.Id,
                                AppliedAt = DateTime.UtcNow,
                                Status = "Interviewing",
                                MatchScore = score,
                                MatchExplanation = explanation,
                                ResumeSnapshotJson = JsonSerializer.Serialize(snapshotObj)
                            };
                            _context.Applications.Add(application);
                            await _context.SaveChangesAsync();

                            // Auto-schedule interview
                            var interviewDate = DateTime.UtcNow.AddDays(new Random().Next(2, 5)).Date.AddHours(10 + new Random().Next(0, 5));
                            var isOnline = score >= 90.0 || new Random().Next(0, 2) == 1;
                            
                            var interview = new Interview
                            {
                                ApplicationId = application.Id,
                                ScheduledTime = interviewDate,
                                LocationOrLink = isOnline ? "https://meet.google.com/ais-recruit-" + Guid.NewGuid().ToString().Substring(0, 8) : "Apex Solutions HR Block, Floor 4, Colombo 03",
                                Notes = $"Auto-scheduled by AI Matching Engine (Compatibility Fit: {score}%). Technical assessment and HR fit meeting.",
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Interviews.Add(interview);
                            await _context.SaveChangesAsync();

                            autoAppliedJobsList.Add(new
                            {
                                jobId = job.Id,
                                title = job.Title,
                                companyName = job.RecruiterProfile?.CompanyName ?? "Unknown Company",
                                score = score,
                                interviewTime = interviewDate,
                                location = interview.LocationOrLink
                            });
                        }
                    }
                }
            }

            return Ok(new
            {
                nextPrompt = nextPrompt,
                isComplete = isComplete,
                extractedDetails = newStateObj.extractedDetails,
                autoAppliedJobs = autoAppliedJobsList
            });
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeResume()
        {
            var candidate = await GetCurrentCandidateAsync();
            if (candidate == null)
            {
                return BadRequest("Only candidates can analyze their profiles.");
            }

            var analysisJson = await _geminiService.AnalyzeResumeAsync(candidate, candidate.User?.FullName ?? "", candidate.User?.Email ?? "");
            candidate.AiAnalysisReportJson = analysisJson;
            await _context.SaveChangesAsync();

            return Ok(JsonSerializer.Deserialize<object>(analysisJson));
        }

        [HttpGet("interview-prep/{jobId}")]
        public async Task<IActionResult> GetInterviewPrep(int jobId)
        {
            var candidate = await GetCurrentCandidateAsync();
            if (candidate == null)
            {
                return BadRequest("Only candidates can get interview preparation tips.");
            }

            var job = await _context.JobPosts.FindAsync(jobId);
            if (job == null)
            {
                return NotFound("Job post not found.");
            }

            var questionsJson = await _geminiService.GenerateInterviewPrepAsync(job, candidate, candidate.User?.FullName ?? "");
            return Ok(JsonSerializer.Deserialize<object>(questionsJson));
        }

        [HttpPost("setup-api-key")]
        public IActionResult SetupApiKey([FromBody] Dictionary<string, string> payload)
        {
            if (payload.TryGetValue("apiKey", out var key) && !string.IsNullOrWhiteSpace(key))
            {
                Environment.SetEnvironmentVariable("GEMINI_API_KEY", key);
                return Ok(new { message = "Gemini API key configured successfully for current session." });
            }
            return BadRequest("Invalid API key provided.");
        }
        
        [HttpPost("update-template")]
        public async Task<IActionResult> UpdateTemplate([FromBody] Dictionary<string, int> payload)
        {
            var candidate = await GetCurrentCandidateAsync();
            if (candidate == null) return BadRequest("Profile not found.");
            
            if (payload.TryGetValue("templateId", out var tId))
            {
                candidate.CvTemplateId = tId;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Template selection updated successfully.", templateId = tId });
            }
            return BadRequest("Invalid template ID.");
        }
    }
}
