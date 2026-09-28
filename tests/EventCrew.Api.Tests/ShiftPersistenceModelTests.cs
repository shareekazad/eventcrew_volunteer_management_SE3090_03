using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EventCrew.Api.Tests;

public class ShiftPersistenceModelTests
{
    [Fact]
    public void ShiftMapsToExistingTableAndForeignKeys()
    {
        var options = new DbContextOptionsBuilder<EventCrewDbContext>()
            .UseNpgsql("Host=localhost;Database=eventcrew_test;Username=eventcrew")
            .Options;

        using var context = new EventCrewDbContext(options);
        var shift = context.Model.FindEntityType(typeof(Shift));

        Assert.NotNull(shift);
        Assert.Equal("shifts", shift.GetTableName());

        var table = StoreObjectIdentifier.Table("shifts", null);
        var mappedColumns = shift.GetProperties()
            .Select(property => property.GetColumnName(table) ?? throw new InvalidOperationException("A Shift property has no column mapping."))
            .ToHashSet();

        Assert.Equal(
            new HashSet<string>
            {
                "id", "event_id", "role_requirement_id", "title", "start_time", "end_time",
                "capacity", "status", "created_at", "updated_at"
            },
            mappedColumns);

        Assert.Collection(
            shift.GetForeignKeys().OrderBy(foreignKey => foreignKey.Properties.Single().GetColumnName(table)),
            foreignKey =>
            {
                Assert.Equal("event_id", foreignKey.Properties.Single().GetColumnName(table));
                Assert.Equal(typeof(Event), foreignKey.PrincipalEntityType.ClrType);
                Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
            },
            foreignKey =>
            {
                Assert.Equal("role_requirement_id", foreignKey.Properties.Single().GetColumnName(table));
                Assert.Equal(typeof(RoleRequirement), foreignKey.PrincipalEntityType.ClrType);
                Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
            });
    }
}