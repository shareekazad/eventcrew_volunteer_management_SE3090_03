using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the EventCrew database.
/// Owns the mapping from C# entities → PostgreSQL schema.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // ---- Student 1 (Event & Venue) ----
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<RoleRequirement> RoleRequirements => Set<RoleRequirement>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<VolunteerProfile> VolunteerProfiles => Set<VolunteerProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<VolunteerSkill> VolunteerSkills => Set<VolunteerSkill>();
    public DbSet<QrCodeToken> QrCodeTokens => Set<QrCodeToken>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

        // ---- Shared AI workflow state ----
    public DbSet<AgentWorkflowRun> AgentWorkflowRuns => Set<AgentWorkflowRun>();
    public DbSet<AgentToolLog> AgentToolLogs => Set<AgentToolLog>();

    // ---- Shared identity ----
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Store enums as their string names in the DB
        // (matches our DDL: 'Draft', 'Beginner', etc.)
        modelBuilder
            .Entity<Event>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder
            .Entity<RoleRequirement>()
            .Property(r => r.MinExperienceLevel)
            .HasConversion<string>();

        modelBuilder
            .Entity<AttendanceRecord>()
            .Property(a => a.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ShiftAssignment>()
            .HasIndex(a => new { a.ShiftId, a.VolunteerId })
            .IsUnique();

        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(a => a.ShiftAssignmentId)
            .IsUnique();

        modelBuilder.Entity<QrCodeToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<VolunteerSkill>()
            .HasKey(skill => new { skill.VolunteerId, skill.SkillId });

        modelBuilder.Entity<ShiftAssignment>()
            .HasOne(a => a.Shift)
            .WithMany(s => s.Assignments)
            .HasForeignKey(a => a.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShiftAssignment>()
            .HasOne(a => a.AttendanceRecord)
            .WithOne(a => a.ShiftAssignment)
            .HasForeignKey<AttendanceRecord>(a => a.ShiftAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QrCodeToken>()
            .HasOne(t => t.Shift)
            .WithMany(s => s.QrCodeTokens)
            .HasForeignKey(t => t.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}