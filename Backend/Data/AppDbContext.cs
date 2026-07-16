using Microsoft.EntityFrameworkCore;
using AITalentHub.Models;

namespace AITalentHub.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public DbSet<RecruiterProfile> RecruiterProfiles { get; set; }
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - CandidateProfile (1-to-1)
            modelBuilder.Entity<CandidateProfile>()
                .HasOne(c => c.User)
                .WithOne(u => u.CandidateProfile)
                .HasForeignKey<CandidateProfile>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - RecruiterProfile (1-to-1)
            modelBuilder.Entity<RecruiterProfile>()
                .HasOne(r => r.User)
                .WithOne(u => u.RecruiterProfile)
                .HasForeignKey<RecruiterProfile>(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // RecruiterProfile - JobPost (1-to-many)
            modelBuilder.Entity<JobPost>()
                .HasOne(j => j.RecruiterProfile)
                .WithMany()
                .HasForeignKey(j => j.RecruiterProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobPost - Application (1-to-many)
            modelBuilder.Entity<Application>()
                .HasOne(a => a.JobPost)
                .WithMany()
                .HasForeignKey(a => a.JobPostId)
                .OnDelete(DeleteBehavior.Cascade);

            // CandidateProfile - Application (1-to-many)
            modelBuilder.Entity<Application>()
                .HasOne(a => a.CandidateProfile)
                .WithMany()
                .HasForeignKey(a => a.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Application - Interview (1-to-many)
            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Application)
                .WithMany()
                .HasForeignKey(i => i.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
