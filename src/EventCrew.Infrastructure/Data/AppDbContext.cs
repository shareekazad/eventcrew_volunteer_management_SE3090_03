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

        // ---- Shared AI workflow state ----
    public DbSet<AgentWorkflowRun> AgentWorkflowRuns => Set<AgentWorkflowRun>();
    public DbSet<AgentToolLog> AgentToolLogs => Set<AgentToolLog>();

    // Shared identity
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
    }
}