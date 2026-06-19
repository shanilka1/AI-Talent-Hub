using System;

namespace AITalentHub.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Candidate" or "Recruiter"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public CandidateProfile? CandidateProfile { get; set; }
        public RecruiterProfile? RecruiterProfile { get; set; }
    }
}
