using System;
using System.Text.Json.Serialization;

namespace AITalentHub.Models
{
    public class Interview
    {
        public int Id { get; set; }
        
        public int ApplicationId { get; set; }
        public Application? Application { get; set; }

        public DateTime ScheduledTime { get; set; }
        public string LocationOrLink { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
        public int CandidateRating { get; set; } = 0; // 0 = not rated, 1-5 = rating score
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
