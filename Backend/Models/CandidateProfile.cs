using System;
using System.Text.Json.Serialization;

namespace AITalentHub.Models
{
    public class CandidateProfile
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [JsonIgnore]
        public User? User { get; set; }

        public string Bio { get; set; } = string.Empty;
        
        public string Skills { get; set; } = string.Empty; // Semicolon-separated values: e.g., "C#;React;SQL"
        
        public string ExperienceJson { get; set; } = "[]"; // JSON array of experience items
        
        public string EducationJson { get; set; } = "[]"; // JSON array of education items
        
        public string ResumePath { get; set; } = string.Empty;
        
        public string? RawResumeText { get; set; } // Holds the raw unparsed text
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
