using Microsoft.EntityFrameworkCore;
using ORUApi.Models;

namespace ORUApi.Data
{
    public class ORUDbContext(DbContextOptions<ORUDbContext> options)
    : DbContext(options)
    {
           public DbSet<Application> Applications => Set<Application>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
     public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<StudyLevel> StudyLevels => Set<StudyLevel>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<AdminActivityLog> AdminActivityLogs => Set<AdminActivityLog>();
        protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AdminActivityLog>()
            .HasOne(l => l.Admin)
            .WithMany()
            .HasForeignKey(l => l.AdminId)
            .OnDelete(DeleteBehavior.Cascade);
        model.Entity<Application>()
            .Property(a => a.StudyLevelId).HasColumnName("StudyLevel");

        model.Entity<Application>()
            .HasOne(a => a.StudyLevelRef)
            .WithMany()
            .HasForeignKey(a => a.StudyLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Application>()
            .HasMany(a => a.Documents)
            .WithOne(d => d.Application)
            .HasForeignKey(d => d.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<Student>()
            .Property(s => s.EnrolledCourses).HasColumnType("jsonb");

        model.Entity<Student>()
            .Property(s => s.PaymentHistory).HasColumnType("jsonb");

        model.Entity<Student>()
            .Property(s => s.InstallmentSubmissions).HasColumnType("jsonb");

        model.Entity<Student>()
            .Ignore(s => s.OutstandingBalance);

        model.Entity<Admin>()
            .Property(a => a.Permissions).HasColumnType("jsonb");

        model.Entity<Announcement>()
            .Property(a => a.ImageUrls).HasColumnType("jsonb");

        model.Entity<Student>().HasIndex(s => s.MatricNumber).IsUnique();
        model.Entity<Student>().HasIndex(s => s.Email).IsUnique();
        model.Entity<Admin>().HasIndex(a => a.Email).IsUnique();
        model.Entity<Admin>().HasIndex(a => a.StaffId).IsUnique();

        model.Entity<Course>().HasIndex(c => c.Code).IsUnique();
    }
    }
}
