using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AITalentHub.Models;
using AITalentHub.Services;

namespace AITalentHub.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            // 0. Ensure Admin exists
            if (!context.Users.Any(u => u.Email == "administrator@admin.com"))
            {
                var adminUser = new User
                {
                    Email = "administrator@admin.com",
                    PasswordHash = PasswordHasher.HashPassword("administrator@admin.com"),
                    FullName = "System Administrator",
                    Role = "Admin"
                };
                context.Users.Add(adminUser);
                context.SaveChanges();
            }

            if (context.Users.Any(u => u.Role != "Admin"))
            {
                return; // Database already contains data other than admin
            }

            // 1. Create Recruiter User
            var recruiterUser = new User
            {
                Email = "recruiter@example.com",
                PasswordHash = PasswordHasher.HashPassword("password123"),
                FullName = "Shanilka Lakshan",
                Role = "Recruiter"
            };
            context.Users.Add(recruiterUser);

            // 2. Create Candidate User
            var candidateUser = new User
            {
                Email = "candidate@example.com",
                PasswordHash = PasswordHasher.HashPassword("password123"),
                FullName = "Kawya Dissanayaka",
                Role = "Candidate"
            };
            context.Users.Add(candidateUser);

            context.SaveChanges();

            // 3. Create Recruiter Company Profile
            var recruiterProfile = new RecruiterProfile
            {
                UserId = recruiterUser.Id,
                CompanyName = "Apex Software Solutions",
                CompanyDescription = "A global leader in smart recruitment solutions and enterprise software engineering.",
                CompanyWebsite = "https://apexsolutions.example.com"
            };
            context.RecruiterProfiles.Add(recruiterProfile);

            // 4. Create Candidate Profile
            var experience = new List<object>
            {
                new { title = "Junior Software Engineer", company = "TechCorp Solutions", years = "2023 - 2025" }
            };
            var education = new List<object>
            {
                new { degree = "B.Sc. in Computer Science", school = "University of Moratuwa", year = "2023" }
            };

            var candidateProfile = new CandidateProfile
            {
                UserId = candidateUser.Id,
                Bio = "Passionate full-stack developer with experience in C# programming, ASP.NET Core REST APIs, and React frontend dashboards.",
                Skills = "C#;ASP.NET Core;React;JavaScript;SQL;Git",
                ExperienceJson = JsonSerializer.Serialize(experience),
                EducationJson = JsonSerializer.Serialize(education)
            };
            context.CandidateProfiles.Add(candidateProfile);

            context.SaveChanges();

            // 5. Create Job Postings
            var job1 = new JobPost
            {
                RecruiterProfileId = recruiterProfile.Id,
                Title = "Senior .NET Developer",
                Description = "We are looking for a Senior .NET developer to build scalable cloud APIs using ASP.NET Core and SQL Server.",
                Requirements = "C#;ASP.NET Core;SQL;Docker;AWS",
                Location = "Colombo, Sri Lanka",
                JobType = "Full-Time",
                SalaryRange = "Rs. 250,000 - 350,000 / Month",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            var job2 = new JobPost
            {
                RecruiterProfileId = recruiterProfile.Id,
                Title = "Frontend UI Engineer (React)",
                Description = "Join our team to build high-performance Web applications using modern React, CSS grids, and clean JavaScript.",
                Requirements = "React;JavaScript;HTML;CSS;Git",
                Location = "Remote",
                JobType = "Remote",
                SalaryRange = "Rs. 180,000 - 240,000 / Month",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            context.JobPosts.AddRange(job1, job2);
            context.SaveChanges();

            // 6. Create Application (Pre-calculated Match)
            var matchingService = new MatchingService();
            var (score, explanation) = matchingService.CalculateMatch(candidateProfile, job1);

            var snapshot = new
            {
                fullName = candidateUser.FullName,
                bio = candidateProfile.Bio,
                skills = candidateProfile.Skills,
                experience = candidateProfile.ExperienceJson,
                education = candidateProfile.EducationJson
            };

            var application = new Application
            {
                JobPostId = job1.Id,
                CandidateProfileId = candidateProfile.Id,
                AppliedAt = DateTime.UtcNow.AddHours(-12),
                Status = "Interviewing",
                MatchScore = score,
                MatchExplanation = explanation,
                ResumeSnapshotJson = JsonSerializer.Serialize(snapshot)
            };
            context.Applications.Add(application);
            context.SaveChanges();

            // 7. Create Scheduled Interview
            var interview = new Interview
            {
                ApplicationId = application.Id,
                ScheduledTime = DateTime.UtcNow.AddDays(2),
                LocationOrLink = "https://meet.google.com/abc-defg-hij",
                Notes = "Technical assessment covering C# algorithms, Entity Framework Core, and database design. Be prepared for live coding.",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };
            context.Interviews.Add(interview);
            context.SaveChanges();
        }
    }
}
