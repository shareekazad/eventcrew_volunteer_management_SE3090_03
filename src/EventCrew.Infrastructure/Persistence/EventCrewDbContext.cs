using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Infrastructure.Persistence;

public class EventCrewDbContext(DbContextOptions<EventCrewDbContext> options) : DbContext(options)
{
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventCrewDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}