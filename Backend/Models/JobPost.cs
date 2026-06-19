using System;
using System.Text.Json.Serialization;

namespace AITalentHub.Models
{
    public class JobPost
    {
        public int Id { get; set; }
        
        public int RecruiterProfileId { get; set; }
        
        [JsonIgnore]
        public RecruiterProfile? RecruiterProfile { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty; // Semicolon-separated skills: e.g., "C#;React;SQL"
        public string Location { get; set; } = string.Empty;
        public string JobType { get; set; } = "Full-Time"; // "Full-Time", "Part-Time", "Remote", "Hybrid"
        public string SalaryRange { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
