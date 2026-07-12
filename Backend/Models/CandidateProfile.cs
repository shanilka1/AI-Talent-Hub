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
        
        public string? ExperienceJson { get; set; } = "[]"; // JSON array of experience items
        
        public string? EducationJson { get; set; } = "[]"; // JSON array of education items
        
        public string? ProjectsJson { get; set; } = "[]"; // JSON array of projects
        
        public string ResumePath { get; set; } = string.Empty;
        
        public string? RawResumeText { get; set; } // Holds the raw unparsed text
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Expanded Fields for Smart Recruitment
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string CertificationsJson { get; set; } = "[]"; // JSON array of certification items
        public string ProjectsJson { get; set; } = "[]"; // JSON array of project items
        public string Languages { get; set; } = string.Empty; // Semicolon-separated values: e.g. "English;Sinhala"
        public string AchievementsJson { get; set; } = "[]"; // JSON array of achievements
        public string ReferencesJson { get; set; } = "[]"; // JSON array of references
        public string CareerObjective { get; set; } = string.Empty;
        public string PreferredJobCategory { get; set; } = string.Empty;
        public string PreferredSalary { get; set; } = string.Empty;
        public string PreferredLocation { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
        public string GitHubUrl { get; set; } = string.Empty;
        
        // Conversational AI Assistant & Analytics State Cache
        public string OnboardingStateJson { get; set; } = "{}"; // Tracks state of onboarding chatbot
        public string AiAnalysisReportJson { get; set; } = "{}"; // Skill Score, Employability analysis, and recommendations
        public int CvTemplateId { get; set; } = 1;
    }
}
