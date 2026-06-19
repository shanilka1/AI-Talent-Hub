using System;
using System.Text.Json.Serialization;

namespace AITalentHub.Models
{
    public class Application
    {
        public int Id { get; set; }
        
        public int JobPostId { get; set; }
        public JobPost? JobPost { get; set; }
        
        public int CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Applied"; // "Applied", "Reviewing", "Interviewing", "Offered", "Rejected"
        
        public double MatchScore { get; set; } = 0.0;
        public string MatchExplanation { get; set; } = string.Empty; // Semicolon-separated details
        
        public string ResumeSnapshotJson { get; set; } = "{}"; // Snapshot of candidate details at application time
    }
}
