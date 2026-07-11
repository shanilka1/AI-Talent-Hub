using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITalentHub.Data;
using AITalentHub.Models;

namespace AITalentHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ResumesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResumesController(AppDbContext context)
        {
            _context = context;
        }

        public class UpdateProfileDto
        {
            public string Bio { get; set; } = string.Empty;
            public string Skills { get; set; } = string.Empty;
            public string ExperienceJson { get; set; } = "[]";
            public string EducationJson { get; set; } = "[]";
        }

        public class ParseRequestDto
        {
            public string ResumeText { get; set; } = string.Empty;
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

        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _context.CandidateProfiles
                                        .Include(c => c.User)
                                        .FirstOrDefaultAsync(c => c.UserId == userId);

            if (profile == null)
            {
                return NotFound("Profile not found.");
            }

            return Ok(new
            {
                id = profile.Id,
                userId = profile.UserId,
                fullName = profile.User?.FullName,
                email = profile.User?.Email,
                bio = profile.Bio,
                skills = profile.Skills,
                experience = JsonSerializer.Deserialize<object>(profile.ExperienceJson),
                education = JsonSerializer.Deserialize<object>(profile.EducationJson),
                resumePath = profile.ResumePath,
                updatedAt = profile.UpdatedAt
            });
        }

        [HttpPut("my-profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);

            if (profile == null)
            {
                return NotFound("Profile not found.");
            }

            profile.Bio = dto.Bio;
            profile.Skills = dto.Skills;
            profile.ExperienceJson = dto.ExperienceJson;
            profile.EducationJson = dto.EducationJson;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully.", profile = profile });
        }

        [HttpPost("parse-text")]
        public async Task<IActionResult> ParseText([FromBody] ParseRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ResumeText))
            {
                return BadRequest("Resume text content is empty.");
            }

            // Simple parser: Scan for common skills and key info
            var commonSkills = new List<string> {
                "C#", ".NET", "ASP.NET Core", "React", "Angular", "Vue", "JavaScript", "TypeScript", 
                "HTML", "CSS", "SQL", "Microsoft SQL Server", "SQLite", "Python", "Java", "C++", 
                "Docker", "Kubernetes", "AWS", "Azure", "DevOps", "Git", "GitHub", "Bootstrap"
            };

            var extractedSkills = new List<string>();
            foreach (var skill in commonSkills)
            {
                // Word boundary check or case-insensitive contains
                if (dto.ResumeText.Contains(skill, StringComparison.OrdinalIgnoreCase))
                {
                    extractedSkills.Add(skill);
                }
            }

            // Clean it up: convert it to Semicolon delimited
            var skillsString = string.Join(";", extractedSkills);

            // Simple heuristic to extract a basic bio or summary from the first 200 chars
            var lines = dto.ResumeText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var summary = lines.Length > 0 ? lines[0] : "Parsed Resume Profile";
            if (lines.Length > 1) summary += " - " + lines[1];
            if (summary.Length > 150) summary = summary.Substring(0, 147) + "...";

            var experiences = new List<object>();
            var educations = new List<object>();
            string currentSection = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Experience", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "Experience";
                    continue;
                }
                else if (trimmed.StartsWith("Education", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "Education";
                    continue;
                }

                if (currentSection == "Experience" && trimmed.StartsWith("-"))
                {
                    var text = trimmed.Substring(1).Trim();
                    var parts = text.Split(new[] { " at ", ":" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        experiences.Add(new { title = parts[0].Trim(), company = parts[1].Trim(), years = "Recent" });
                    }
                    else
                    {
                        experiences.Add(new { title = text, company = "Unknown", years = "Recent" });
                    }
                }
                else if (currentSection == "Education" && trimmed.Length > 5)
                {
                    var parts = trimmed.Split(new[] { " from " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        educations.Add(new { degree = parts[0].Trim(), school = parts[1].Trim(), year = "Unknown" });
                    }
                    else
                    {
                        educations.Add(new { degree = trimmed, school = "Unknown", year = "Unknown" });
                    }
                }
            }

            // Update candidate profile with these parsed values automatically!
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (profile != null)
            {
                profile.Skills = string.Join(";", extractedSkills);
                
                if (lines.Length > 0 && lines[0].Split(' ').Length <= 4)
                {
                    if (user != null)
                    {
                        user.FullName = lines[0].Trim();
                    }
                }

                profile.Bio = $"Experienced professional. Parsed Summary: {summary}";

                if (experiences.Count > 0)
                {
                    profile.ExperienceJson = JsonSerializer.Serialize(experiences);
                }

                if (educations.Count > 0)
                {
                    profile.EducationJson = JsonSerializer.Serialize(educations);
                }

                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                extractedSkills = extractedSkills,
                summary = summary,
                message = "Resume successfully parsed and skills updated in your profile."
            });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest("Only .pdf, .doc, .docx, and .txt files are allowed.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Also update candidate profile's ResumePath
            var userId = GetCurrentUserId();
            var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            if (profile != null)
            {
                profile.ResumePath = uniqueFileName;
                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { filename = uniqueFileName, filePath = $"/uploads/{uniqueFileName}" });
        }
    }
}
