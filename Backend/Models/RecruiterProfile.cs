using System;
using System.Text.Json.Serialization;

namespace AITalentHub.Models
{
    public class RecruiterProfile
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [JsonIgnore]
        public User? User { get; set; }

        public string CompanyName { get; set; } = string.Empty;
        public string CompanyDescription { get; set; } = string.Empty;
        public string CompanyWebsite { get; set; } = string.Empty;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
