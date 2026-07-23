using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ResumesController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public class UpdateProfileDto
        {
            public string Bio { get; set; } = string.Empty;
            public string Skills { get; set; } = string.Empty;
            public string ExperienceJson { get; set; } = "[]";
            public string EducationJson { get; set; } = "[]";
            public string ProjectsJson { get; set; } = "[]";
            public string? RawResumeText { get; set; }
        }

        public class ParseRequestDto
        {
            public string ResumeText { get; set; } = string.Empty;
        }

        public class GeminiParsedResume
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
            [JsonPropertyName("bio")]
            public string Bio { get; set; } = string.Empty;
            [JsonPropertyName("skills")]
            public List<string> Skills { get; set; } = new List<string>();
            [JsonPropertyName("experience")]
            public List<object> Experience { get; set; } = new List<object>();
            [JsonPropertyName("education")]
            public List<object> Education { get; set; } = new List<object>();
            [JsonPropertyName("projects")]
            public List<object> Projects { get; set; } = new List<object>();
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
                experience = JsonSerializer.Deserialize<object>(profile.ExperienceJson ?? "[]"),
                education = JsonSerializer.Deserialize<object>(profile.EducationJson ?? "[]"),
                projects = JsonSerializer.Deserialize<object>(profile.ProjectsJson ?? "[]"),
                resumePath = profile.ResumePath,
                rawResumeText = profile.RawResumeText,
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
            profile.ExperienceJson = dto.ExperienceJson ?? "[]";
            profile.EducationJson = dto.EducationJson ?? "[]";
            profile.ProjectsJson = dto.ProjectsJson ?? "[]";
            
            if (!string.IsNullOrEmpty(dto.RawResumeText))
            {
                profile.RawResumeText = dto.RawResumeText;
            }

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

            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return StatusCode(500, "Gemini API Key is not configured.");
            }

            var prompt = @"You are an expert resume parser. Extract the following information and output EXACTLY in the JSON format below. Do not include markdown or backticks, just the JSON.
{
  ""name"": ""Full Name"",
  ""bio"": ""A short 1-2 sentence professional summary."",
  ""skills"": [""Skill1"", ""Skill2""],
  ""experience"": [ { ""title"": ""Job Title"", ""company"": ""Company Name"", ""years"": ""e.g. 2020-2023"" } ],
  ""education"": [ { ""degree"": ""Degree Name"", ""school"": ""University"", ""year"": ""e.g. 2019"" } ],
  ""projects"": [ { ""name"": ""Project Name"", ""description"": ""Short description"", ""technologies"": ""Tech stack used"" } ]
}
Resume Text:
" + dto.ResumeText;

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                try
                {
                    using var errorDoc = JsonDocument.Parse(error);
                    var message = errorDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
                    return StatusCode((int)response.StatusCode, $"AI API Error: {message}");
                }
                catch
                {
                    return StatusCode((int)response.StatusCode, $"AI Parsing failed: {error}");
                }
            }

            var responseString = await response.Content.ReadAsStringAsync();
            try
            {
                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;
                var textResponse = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";

                // Clean up possible markdown code blocks
                if (textResponse.StartsWith("```json")) textResponse = textResponse.Substring(7);
                else if (textResponse.StartsWith("```")) textResponse = textResponse.Substring(3);
                if (textResponse.EndsWith("```")) textResponse = textResponse.Substring(0, textResponse.Length - 3);
                textResponse = textResponse.Trim();

                var parsed = JsonSerializer.Deserialize<GeminiParsedResume>(textResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed == null) throw new Exception("Failed to deserialize JSON.");

                // Update database
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);
                var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (profile != null)
                {
                    if (!string.IsNullOrWhiteSpace(parsed.Name) && user != null)
                    {
                        user.FullName = parsed.Name;
                    }

                    if (parsed.Skills != null && parsed.Skills.Count > 0)
                    {
                        profile.Skills = string.Join(";", parsed.Skills);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(parsed.Bio))
                    {
                        profile.Bio = parsed.Bio;
                    }

                    if (parsed.Experience != null)
                    {
                        profile.ExperienceJson = JsonSerializer.Serialize(parsed.Experience);
                    }

                    if (parsed.Education != null)
                    {
                        profile.EducationJson = JsonSerializer.Serialize(parsed.Education);
                    }

                    if (parsed.Projects != null)
                    {
                        profile.ProjectsJson = JsonSerializer.Serialize(parsed.Projects);
                    }

                    profile.RawResumeText = dto.ResumeText;
                    profile.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Resume successfully parsed via AI.",
                    extractedSkills = parsed.Skills,
                    summary = parsed.Bio
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing AI response: {ex.Message}");
            }
        }

        [HttpPost("parse-file")]
        public async Task<IActionResult> ParseFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var allowedExtensions = new[] { ".pdf", ".txt" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest("Only .pdf and .txt files are supported for AI parsing.");
            }

            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return StatusCode(500, "Gemini API Key is not configured.");
            }

            try
            {
                object requestBody;
                var prompt = @"You are an expert resume parser. Extract the following information and output EXACTLY in the JSON format below. Do not include markdown or backticks, just the JSON.
{
  ""name"": ""Full Name"",
  ""bio"": ""A short 1-2 sentence professional summary."",
  ""skills"": [""Skill1"", ""Skill2""],
  ""experience"": [ { ""title"": ""Job Title"", ""company"": ""Company Name"", ""years"": ""e.g. 2020-2023"" } ],
  ""education"": [ { ""degree"": ""Degree Name"", ""school"": ""University"", ""year"": ""e.g. 2019"" } ],
  ""projects"": [ { ""name"": ""Project Name"", ""description"": ""Short description"", ""technologies"": ""Tech stack used"" } ]
}";

                if (ext == ".pdf")
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    var base64Data = Convert.ToBase64String(memoryStream.ToArray());

                    requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new object[]
                                {
                                    new { text = prompt },
                                    new { inlineData = new { mimeType = "application/pdf", data = base64Data } }
                                }
                            }
                        }
                    };
                }
                else
                {
                    // For text file
                    using var reader = new StreamReader(file.OpenReadStream());
                    var text = await reader.ReadToEndAsync();
                    
                    requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[] { new { text = prompt + "\n\nResume Text:\n" + text } }
                            }
                        }
                    };
                }

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(error);
                        var message = errorDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
                        return StatusCode((int)response.StatusCode, $"AI API Error: {message}");
                    }
                    catch
                    {
                        return StatusCode((int)response.StatusCode, $"AI Parsing failed: {error}");
                    }
                }

                var responseString = await response.Content.ReadAsStringAsync();
                
                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;
                var textResponse = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";

                // Clean up possible markdown code blocks
                if (textResponse.StartsWith("```json")) textResponse = textResponse.Substring(7);
                else if (textResponse.StartsWith("```")) textResponse = textResponse.Substring(3);
                if (textResponse.EndsWith("```")) textResponse = textResponse.Substring(0, textResponse.Length - 3);
                textResponse = textResponse.Trim();

                var parsed = JsonSerializer.Deserialize<GeminiParsedResume>(textResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed == null) throw new Exception("Failed to deserialize JSON.");

                // Update database
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);
                var profile = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (profile != null)
                {
                    if (!string.IsNullOrWhiteSpace(parsed.Name) && user != null)
                    {
                        user.FullName = parsed.Name;
                    }

                    if (parsed.Skills != null && parsed.Skills.Count > 0)
                    {
                        profile.Skills = string.Join(";", parsed.Skills);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(parsed.Bio))
                    {
                        profile.Bio = parsed.Bio;
                    }

                    if (parsed.Experience != null)
                    {
                        profile.ExperienceJson = JsonSerializer.Serialize(parsed.Experience);
                    }

                    if (parsed.Education != null)
                    {
                        profile.EducationJson = JsonSerializer.Serialize(parsed.Education);
                    }

                    if (parsed.Projects != null)
                    {
                        profile.ProjectsJson = JsonSerializer.Serialize(parsed.Projects);
                    }

                    profile.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "File successfully parsed via AI.",
                    extractedData = parsed
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing AI response: {ex.Message}");
            }
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
