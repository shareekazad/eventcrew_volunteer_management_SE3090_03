using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EventCrew.Api.Tests;

public class AttendancePersistenceModelTests
{
    [Fact]
    public void AttendanceAndQrEntitiesMapToExistingTablesAndColumns()
    {
        var options = new DbContextOptionsBuilder<EventCrewDbContext>().UseNpgsql("Host=localhost;Database=eventcrew_test;Username=eventcrew").Options;
        using var context = new EventCrewDbContext(options);
        AssertColumns<AttendanceRecord>(context, "attendance_records", ["id", "shift_assignment_id", "check_in_time", "check_out_time", "check_in_latitude", "check_in_longitude", "status", "verified_hours", "created_at", "updated_at"]);
        AssertColumns<QrCodeToken>(context, "qr_code_tokens", ["id", "shift_id", "token_hash", "expires_at", "is_active", "created_at"]);
        var attendance = context.Model.FindEntityType(typeof(AttendanceRecord))!;
        Assert.Contains(attendance.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(AttendanceRecord.ShiftAssignmentId));
        Assert.Contains(attendance.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(ShiftAssignment) && fk.DeleteBehavior == DeleteBehavior.Cascade);
        Assert.Contains(context.Model.FindEntityType(typeof(QrCodeToken))!.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(Shift) && fk.DeleteBehavior == DeleteBehavior.Cascade);
    }

    private static void AssertColumns<T>(EventCrewDbContext context, string tableName, string[] expected) where T : class
    {
        var entity = context.Model.FindEntityType(typeof(T))!;
        Assert.Equal(tableName, entity.GetTableName());
        var table = StoreObjectIdentifier.Table(tableName, null);
        Assert.Equal(expected.ToHashSet(), entity.GetProperties().Select(property => property.GetColumnName(table)!).ToHashSet());
    }
}
