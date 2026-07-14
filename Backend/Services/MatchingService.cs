using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AITalentHub.Models;

namespace AITalentHub.Services
{
    public interface IMatchingService
    {
        (double Score, string ExplanationJson) CalculateMatch(CandidateProfile candidate, JobPost job);
    }

    public class MatchingService : IMatchingService
    {
        public (double Score, string ExplanationJson) CalculateMatch(CandidateProfile candidate, JobPost job)
        {
            if (string.IsNullOrWhiteSpace(job.Requirements))
            {
                return (100.0, JsonSerializer.Serialize(new { summary = "No specific requirements listed for this job.", matches = new List<string>(), missing = new List<string>() }));
            }

            var requirements = job.Requirements.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(r => r.Trim())
                                              .ToList();

            var candidateSkills = candidate.Skills.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(s => s.Trim())
                                                  .ToList();

            var matchedSkills = new List<string>();
            var missingSkills = new List<string>();
            var explanationNotes = new List<string>();

            // Convert experience and education to lowercase text for keyword searches
            string candidateText = $"{candidate.Bio} {candidate.ExperienceJson} {candidate.EducationJson}".ToLower();

            foreach (var req in requirements)
            {
                bool isMatch = false;

                // 1. Direct skill match (case insensitive)
                var directMatch = candidateSkills.FirstOrDefault(s => s.Equals(req, StringComparison.OrdinalIgnoreCase));
                if (directMatch != null)
                {
                    isMatch = true;
                    matchedSkills.Add(req);
                    explanationNotes.Add($"Matches required skill: **{req}**");
                }
                // 2. Keyword match in experience/education/bio
                else if (candidateText.Contains(req.ToLower()))
                {
                    isMatch = true;
                    matchedSkills.Add(req);
                    explanationNotes.Add($"Found keyword **{req}** mentioned in your work experience or education.");
                }
                else
                {
                    missingSkills.Add(req);
                    explanationNotes.Add($"Missing required skill: **{req}**");
                }
            }

            double score = (double)matchedSkills.Count / requirements.Count * 100;
            score = Math.Round(score, 1);

            var result = new
            {
                score = score,
                summary = $"Candidate matches {matchedSkills.Count} out of {requirements.Count} job requirements ({score}%).",
                matches = matchedSkills,
                missing = missingSkills,
                details = explanationNotes
            };

            return (score, JsonSerializer.Serialize(result));
        }
    }
}
