using EventCrew.Api.Controllers;
using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EventCrew.Api.Tests;

public class AttendanceControllerTests
{
    [Fact]
    public async Task CreateQrTokenReturnsSecretAndStoresOnlyHashThenCheckinWorks()
    {
        await using var db = CreateContext();
        var data = await SeedAsync(db);
        var createdToken = Assert.IsType<CreatedAtActionResult>((await new QrTokenController(db).Create(data.Shift.Id, CancellationToken.None)).Result);
        var response = Assert.IsType<QrTokenResponse>(createdToken.Value);
        var stored = await db.QrCodeTokens.SingleAsync();
        Assert.NotEqual(response.Token, stored.TokenHash);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(response.Token))), stored.TokenHash);
        var checkin = await VolunteerController(db).CheckIn(Request(response.Token, data), CancellationToken.None);
        Assert.IsType<OkObjectResult>(checkin.Result);
        var attendance = await db.AttendanceRecords.SingleAsync();
        Assert.Equal("CheckedIn", attendance.Status);
        Assert.NotNull(attendance.CheckInTime);
        Assert.Equal(data.Assignment.Id, attendance.ShiftAssignmentId);
    }

    [Fact]
    public async Task CheckInRejectsExpiredInactiveWrongShiftAndDuplicateTokens()
    {
        await using var db = CreateContext();
        var data = await SeedAsync(db);
        var attendance = VolunteerController(db);
        var expired = await AddTokenAsync(db, data.Shift.Id, "expired", DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.IsType<ConflictObjectResult>((await attendance.CheckIn(Request("expired", data), CancellationToken.None)).Result);
        expired.IsActive = false;
        expired.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>((await attendance.CheckIn(Request("expired", data), CancellationToken.None)).Result);
        var otherShift = Guid.NewGuid();
        await AddTokenAsync(db, otherShift, "other", DateTimeOffset.UtcNow.AddHours(1));
        Assert.IsType<ConflictObjectResult>((await attendance.CheckIn(Request("other", data), CancellationToken.None)).Result);
        var valid = await AddTokenAsync(db, data.Shift.Id, "valid", DateTimeOffset.UtcNow.AddHours(1));
        valid.IsActive = true;
        await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>((await attendance.CheckIn(Request("valid", data), CancellationToken.None)).Result);
        Assert.IsType<ConflictObjectResult>((await attendance.CheckIn(Request("valid", data), CancellationToken.None)).Result);
    }

    [Fact]
    public async Task CheckInReturnsNotFoundWhenSuppliedDemoProfileHasNoAssignment()
    {
        await using var db = CreateContext();
        var data = await SeedAsync(db);
        await AddTokenAsync(db, data.Shift.Id, "valid", DateTimeOffset.UtcNow.AddHours(1));
        var otherProfile = new VolunteerProfile
        {
            Id = Guid.NewGuid(),
            FullName = "Other volunteer",
            Email = "other@example.test"
        };
        db.VolunteerProfiles.Add(otherProfile);
        await db.SaveChangesAsync();
        var request = new AttendanceActionRequest { ShiftId = data.Shift.Id, VolunteerId = otherProfile.Id, Token = "valid" };
        Assert.IsType<NotFoundObjectResult>((await VolunteerController(db).CheckIn(request, CancellationToken.None)).Result);
        Assert.Empty(await db.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task CheckoutRequiresCheckinAndRejectsDuplicateCheckout()
    {
        await using var db = CreateContext();
        var data = await SeedAsync(db);
        await AddTokenAsync(db, data.Shift.Id, "valid", DateTimeOffset.UtcNow.AddHours(1));
        var controller = VolunteerController(db);
        var request = Request("valid", data);
        Assert.IsType<NotFoundObjectResult>((await controller.CheckOut(request, CancellationToken.None)).Result);
        await controller.CheckIn(request, CancellationToken.None);
        Assert.IsType<OkObjectResult>((await controller.CheckOut(request, CancellationToken.None)).Result);
        Assert.IsType<ConflictObjectResult>((await controller.CheckOut(request, CancellationToken.None)).Result);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal("CheckedOut", record.Status);
        Assert.True(record.VerifiedHours >= 0);
    }

    [Fact]
    public async Task AttendanceCanBeRetrievedByShiftAndId()
    {
        await using var db = CreateContext();
        var data = await SeedAsync(db);
        var now = DateTimeOffset.UtcNow;
        var record = new AttendanceRecord { Id = Guid.NewGuid(), ShiftAssignmentId = data.Assignment.Id, ShiftAssignment = data.Assignment, CheckInTime = now, Status = "CheckedIn", CreatedAt = now, UpdatedAt = now };
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync();
        var controller = new AttendanceController(db);
        var byShift = Assert.IsType<OkObjectResult>((await controller.GetByShift(data.Shift.Id, CancellationToken.None)).Result).Value as IReadOnlyList<AttendanceResponse>;
        Assert.Single(byShift!);
        Assert.Equal(data.Volunteer.Id, byShift![0].VolunteerId);
        Assert.IsType<OkObjectResult>((await controller.GetById(record.Id, CancellationToken.None)).Result);
    }

    private static AttendanceActionRequest Request(string token, SeedData data) => new() { Token = token, ShiftId = data.Shift.Id, VolunteerId = data.Volunteer.Id };

    private static async Task<QrCodeToken> AddTokenAsync(EventCrewDbContext db, Guid shiftId, string token, DateTimeOffset expires) 
    {
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
        var entity = new QrCodeToken { Id = Guid.NewGuid(), ShiftId = shiftId, TokenHash = tokenHash, ExpiresAt = expires, IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
        db.QrCodeTokens.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    private static EventCrewDbContext CreateContext() => new(new DbContextOptionsBuilder<EventCrewDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static async Task<SeedData> SeedAsync(EventCrewDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var organizerId = Guid.NewGuid();
        var ev = new Event { Id = Guid.NewGuid(), OrganizerId = organizerId, Title = "Event" };
        var requirement = new RoleRequirement { Id = Guid.NewGuid(), EventId = ev.Id, Event = ev, RoleName = "Guide", RequiredHeadcount = 1 };
        var shift = new Shift { Id = Guid.NewGuid(), EventId = ev.Id, RoleRequirementId = requirement.Id, Event = ev, RoleRequirement = requirement, Title = "Morning", StartTime = now.AddHours(1), EndTime = now.AddHours(2), Capacity = 1, Status = "Scheduled", CreatedAt = now, UpdatedAt = now };
        var volunteer = new VolunteerProfile { Id = Guid.NewGuid(), FullName = "Ada Volunteer", Email = "ada@example.test" };
        var assignment = new ShiftAssignment { Id = Guid.NewGuid(), ShiftId = shift.Id, Shift = shift, VolunteerId = volunteer.Id, Volunteer = volunteer, Status = "Confirmed", AssignedAt = now, CreatedAt = now, UpdatedAt = now };
        db.Events.Add(ev); db.RoleRequirements.Add(requirement); db.Shifts.Add(shift); db.VolunteerProfiles.Add(volunteer); db.ShiftAssignments.Add(assignment);
        await db.SaveChangesAsync();
        return new SeedData(shift, volunteer, assignment, organizerId);
    }

    private static AttendanceController VolunteerController(EventCrewDbContext db) =>
        new(db);

    private sealed record SeedData(Shift Shift, VolunteerProfile Volunteer, ShiftAssignment Assignment, Guid OrganizerId);
}
