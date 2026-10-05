using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Infrastructure.Persistence;

public class EventCrewDbContext(DbContextOptions<EventCrewDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<RoleRequirement> RoleRequirements => Set<RoleRequirement>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<VolunteerProfile> VolunteerProfiles => Set<VolunteerProfile>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<QrCodeToken> QrCodeTokens => Set<QrCodeToken>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventCrewDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
