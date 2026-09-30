using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Infrastructure.Data;

/// <summary>
/// EF Core database context for the EventCrew application.
/// Targets PostgreSQL via Npgsql.
/// Combines models from Student 1 (Events & Venues), Student 2 (Volunteers & Applications), and shared infrastructure.
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

    // ---- Student 2: Volunteer Profiles & Applications ----
    public DbSet<VolunteerProfile> VolunteerProfiles => Set<VolunteerProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<VolunteerSkill> VolunteerSkills => Set<VolunteerSkill>();
    public DbSet<Application> Applications => Set<Application>();

    // ---- Shared AI workflow state ----
    public DbSet<AgentWorkflowRun> AgentWorkflowRuns => Set<AgentWorkflowRun>();
    public DbSet<AgentToolLog> AgentToolLogs => Set<AgentToolLog>();

    // ---- Shared identity ----
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Student 1: Enums & Mappings ───────────────────────────────────────
        // Store enums as their string names in the DB (matches DDL: 'Draft', 'Beginner', etc.)
        modelBuilder
            .Entity<Event>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder
            .Entity<RoleRequirement>()
            .Property(r => r.MinExperienceLevel)
            .HasConversion<string>();

        // ── Student 2: Skill ──────────────────────────────────────────────────
        modelBuilder.Entity<Skill>(e =>
        {
            e.ToTable("skills");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(s => s.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
            e.Property(s => s.Description).HasColumnName("description");

            // Name must be unique (matches DB schema)
            e.HasIndex(s => s.Name).IsUnique();
        });

        // ── Student 2: VolunteerProfile ───────────────────────────────────────
        modelBuilder.Entity<VolunteerProfile>(e =>
        {
            e.ToTable("volunteer_profiles");
            e.HasKey(vp => vp.Id);
            e.Property(vp => vp.Id).HasColumnName("id");
            e.Property(vp => vp.UserId).HasColumnName("user_id");
            e.Property(vp => vp.EmergencyContact).HasColumnName("emergency_contact").HasMaxLength(20).IsRequired();
            e.Property(vp => vp.Bio).HasColumnName("bio");
            e.Property(vp => vp.MaxHoursPerWeek).HasColumnName("max_hours_per_week").HasDefaultValue(20);
            e.Property(vp => vp.RatingScore).HasColumnName("rating_score").HasPrecision(3, 2).HasDefaultValue(5.00m);
            e.Property(vp => vp.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now() AT TIME ZONE 'utc'");
            e.Property(vp => vp.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("now() AT TIME ZONE 'utc'");

            // Each user can have exactly one volunteer profile
            e.HasIndex(vp => vp.UserId).IsUnique();

            // FK: VolunteerProfile → User
            e.HasOne(vp => vp.User)
                .WithOne(u => u.VolunteerProfile)
                .HasForeignKey<VolunteerProfile>(vp => vp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Student 2: VolunteerSkill (join entity) ───────────────────────────
        modelBuilder.Entity<VolunteerSkill>(e =>
        {
            e.ToTable("volunteer_skills");

            // Composite primary key
            e.HasKey(vs => new { vs.VolunteerId, vs.SkillId });

            e.Property(vs => vs.VolunteerId).HasColumnName("volunteer_id");
            e.Property(vs => vs.SkillId).HasColumnName("skill_id");
            e.Property(vs => vs.ProficiencyLevel)
                .HasColumnName("proficiency_level")
                .HasMaxLength(20)
                .HasDefaultValue("Intermediate");

            // FK: VolunteerSkill → VolunteerProfile
            e.HasOne(vs => vs.Volunteer)
                .WithMany(vp => vp.VolunteerSkills)
                .HasForeignKey(vs => vs.VolunteerId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK: VolunteerSkill → Skill
            e.HasOne(vs => vs.Skill)
                .WithMany(s => s.VolunteerSkills)
                .HasForeignKey(vs => vs.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Student 2: Application ────────────────────────────────────────────
        modelBuilder.Entity<Application>(e =>
        {
            e.ToTable("applications");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.EventId).HasColumnName("event_id");
            e.Property(a => a.VolunteerId).HasColumnName("volunteer_id");
            e.Property(a => a.RoleRequirementId).HasColumnName("role_requirement_id");
            e.Property(a => a.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("Submitted");
            e.Property(a => a.Notes).HasColumnName("notes");
            e.Property(a => a.AppliedAt)
                .HasColumnName("applied_at")
                .HasDefaultValueSql("now() AT TIME ZONE 'utc'");
            e.Property(a => a.ReviewedAt).HasColumnName("reviewed_at");
            e.Property(a => a.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now() AT TIME ZONE 'utc'");
            e.Property(a => a.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("now() AT TIME ZONE 'utc'");

            // UNIQUE constraint: one application per volunteer per event (matches DB schema)
            e.HasIndex(a => new { a.EventId, a.VolunteerId })
                .IsUnique()
                .HasDatabaseName("uq_event_volunteer_app");

            // FK: Application → Event
            e.HasOne(a => a.Event)
                .WithMany(ev => ev.Applications)
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK: Application → VolunteerProfile
            e.HasOne(a => a.Volunteer)
                .WithMany(vp => vp.Applications)
                .HasForeignKey(a => a.VolunteerId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK: Application → RoleRequirement (optional)
            e.HasOne(a => a.RoleRequirement)
                .WithMany(r => r.Applications)
                .HasForeignKey(a => a.RoleRequirementId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
