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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
