using System.Security.Cryptography;
using System.Text;
using EventCrew.Api.DTOs.Attendance;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using EventCrew.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Tests.Services;

public class AttendanceServiceTests
{
    [Fact]
    public async Task CreateQrTokenAsync_StoresOnlyHash_AndAllowsCheckIn()
    {
        using var fixture = await CreateFixtureAsync();
        var service = new AttendanceService(fixture.Db);

        var token = await service.CreateQrTokenAsync(new CreateQrTokenRequestDto
        {
            ShiftId = fixture.Shift.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        });

        token.Should().NotBeNull();
        token!.Token.Should().NotBeNullOrWhiteSpace();
        token.Token.Should().NotContain(fixture.Shift.Id.ToString());
        var storedToken = await fixture.Db.QrCodeTokens.SingleAsync();
        storedToken.TokenHash.Should().NotBe(token.Token);

        var attendance = await service.CheckInAsync(fixture.CheckIn(token.Token));
        attendance.Status.Should().Be(nameof(AttendanceStatus.CheckedIn));
        attendance.CheckInTime.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckInAsync_RejectsExpiredQrToken()
    {
        using var fixture = await CreateFixtureAsync();
        const string token = "expired-test-token-value-0123456789";
        fixture.Db.QrCodeTokens.Add(new QrCodeToken
        {
            ShiftId = fixture.Shift.Id,
            TokenHash = HashToken(token),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await fixture.Db.SaveChangesAsync();

        var act = () => new AttendanceService(fixture.Db).CheckInAsync(fixture.CheckIn(token));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task CheckInAsync_RejectsInvalidQrToken()
    {
        using var fixture = await CreateFixtureAsync();

        var act = () => new AttendanceService(fixture.Db)
            .CheckInAsync(fixture.CheckIn("invalid-test-token-value-0123456789"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid*");
    }

    [Fact]
    public async Task CheckInAsync_RejectsTokenFromDifferentEventAndShift()
    {
        using var fixture = await CreateFixtureAsync();
        var otherEvent = new Event
        {
            OrganizerId = Guid.NewGuid(),
            Title = "Other Event",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddHours(2)
        };
        var otherShift = new Shift
        {
            EventId = otherEvent.Id,
            RoleRequirementId = Guid.NewGuid(),
            Title = "Other Shift",
            StartTime = otherEvent.StartDate,
            EndTime = otherEvent.EndDate
        };
        fixture.Db.Events.Add(otherEvent);
        fixture.Db.Shifts.Add(otherShift);
        fixture.Db.ShiftAssignments.Add(new ShiftAssignment
        {
            ShiftId = otherShift.Id,
            VolunteerId = fixture.Volunteer.Id,
            Status = "Confirmed",
            Shift = otherShift
        });
        await fixture.Db.SaveChangesAsync();
        var token = await new AttendanceService(fixture.Db).CreateQrTokenAsync(new CreateQrTokenRequestDto
        {
            ShiftId = fixture.Shift.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        });

        var request = fixture.CheckIn(token!.Token);
        request.EventId = otherEvent.Id;
        request.ShiftId = otherShift.Id;
        var act = () => new AttendanceService(fixture.Db).CheckInAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not valid for this shift or event*");
    }

    [Fact]
    public async Task CheckInAsync_RejectsDuplicateCheckIn()
    {
        using var fixture = await CreateFixtureAsync();
        var service = new AttendanceService(fixture.Db);
        var token = await CreateTokenAsync(fixture, service);
        await service.CheckInAsync(fixture.CheckIn(token));

        var act = () => service.CheckInAsync(fixture.CheckIn(token));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*state that allows check-in*");
    }

    [Fact]
    public async Task CheckOutAsync_SetsServerTimesAndVerifiedHours()
    {
        using var fixture = await CreateFixtureAsync();
        var service = new AttendanceService(fixture.Db);
        var token = await CreateTokenAsync(fixture, service);
        var checkedIn = await service.CheckInAsync(fixture.CheckIn(token));

        var checkedOut = await service.CheckOutAsync(
            checkedIn.Id!.Value,
            new CheckOutRequestDto { VolunteerId = fixture.Volunteer.Id });

        checkedOut.Should().NotBeNull();
        checkedOut!.Status.Should().Be(nameof(AttendanceStatus.CheckedOut));
        checkedOut.CheckOutTime.Should().NotBeNull();
        checkedOut.CheckOutTime.Should().BeOnOrAfter(checkedIn.CheckInTime!.Value);
        checkedOut.VerifiedHours.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CheckOutAsync_RejectsDuplicateCheckOut()
    {
        using var fixture = await CreateFixtureAsync();
        var service = new AttendanceService(fixture.Db);
        var token = await CreateTokenAsync(fixture, service);
        var attendance = await service.CheckInAsync(fixture.CheckIn(token));
        var request = new CheckOutRequestDto { VolunteerId = fixture.Volunteer.Id };
        await service.CheckOutAsync(attendance.Id!.Value, request);

        var act = () => service.CheckOutAsync(attendance.Id!.Value, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*state that allows check-out*");
    }

    [Fact]
    public async Task CheckOutAsync_RejectsAttendanceWithoutCheckIn()
    {
        using var fixture = await CreateFixtureAsync();
        var record = new AttendanceRecord
        {
            ShiftAssignmentId = fixture.Assignment.Id,
            Status = AttendanceStatus.Pending
        };
        fixture.Db.AttendanceRecords.Add(record);
        await fixture.Db.SaveChangesAsync();

        var act = () => new AttendanceService(fixture.Db).CheckOutAsync(
            record.Id,
            new CheckOutRequestDto { VolunteerId = fixture.Volunteer.Id });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*state that allows check-out*");
    }

    [Fact]
    public async Task CheckOutAsync_RejectsDifferentVolunteer()
    {
        using var fixture = await CreateFixtureAsync();
        var service = new AttendanceService(fixture.Db);
        var token = await CreateTokenAsync(fixture, service);
        var attendance = await service.CheckInAsync(fixture.CheckIn(token));

        var act = () => service.CheckOutAsync(
            attendance.Id!.Value,
            new CheckOutRequestDto { VolunteerId = Guid.NewGuid() });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to this attendance record*");
    }

    [Fact]
    public async Task GetAttendanceAsync_ListsByEventAndStatisticsCountRecordsAndHours()
    {
        using var fixture = await CreateFixtureAsync();
        var service = new AttendanceService(fixture.Db);
        var token = await CreateTokenAsync(fixture, service);
        var attendance = await service.CheckInAsync(fixture.CheckIn(token));
        await service.CheckOutAsync(
            attendance.Id!.Value,
            new CheckOutRequestDto { VolunteerId = fixture.Volunteer.Id });

        var rows = await service.GetAttendanceAsync(fixture.Event.Id, fixture.Shift.Id, null);
        var statistics = await service.GetStatisticsAsync(fixture.Event.Id, fixture.Shift.Id);

        rows.Should().ContainSingle().Which.VolunteerId.Should().Be(fixture.Volunteer.Id);
        statistics.Should().NotBeNull();
        statistics!.Total.Should().Be(1);
        statistics.CheckedOut.Should().Be(1);
        statistics.VerifiedHours.Should().Be(rows!.Single().VerifiedHours);
    }

    [Fact]
    public async Task GetAttendanceAsync_IncludesConfirmedAssignmentsAsPending()
    {
        using var fixture = await CreateFixtureAsync();
        var service = new AttendanceService(fixture.Db);

        var rows = await service.GetAttendanceAsync(fixture.Event.Id, fixture.Shift.Id, null);
        var statistics = await service.GetStatisticsAsync(fixture.Event.Id, fixture.Shift.Id);

        rows.Should().ContainSingle();
        rows!.Single().Id.Should().BeNull();
        rows.Single().Status.Should().Be(nameof(AttendanceStatus.Pending));
        statistics.Should().NotBeNull();
        statistics!.Total.Should().Be(1);
        statistics.Pending.Should().Be(1);
    }

    [Fact]
    public async Task CheckOutAsync_ReturnsNullForMissingAttendance()
    {
        using var fixture = await CreateFixtureAsync();

        var result = await new AttendanceService(fixture.Db).CheckOutAsync(
            Guid.NewGuid(),
            new CheckOutRequestDto { VolunteerId = fixture.Volunteer.Id });

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAttendanceAsync_ReturnsNullForMissingEvent()
    {
        using var fixture = await CreateFixtureAsync();

        var result = await new AttendanceService(fixture.Db)
            .GetAttendanceAsync(Guid.NewGuid(), null, null);

        result.Should().BeNull();
    }

    private static async Task<string> CreateTokenAsync(Fixture fixture, AttendanceService service)
    {
        var token = await service.CreateQrTokenAsync(new CreateQrTokenRequestDto
        {
            ShiftId = fixture.Shift.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        });
        return token!.Token;
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var db = TestDbFactory.Create();
        var eventEntity = new Event
        {
            OrganizerId = Guid.NewGuid(),
            Title = "Attendance Test",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddHours(2)
        };
        var shift = new Shift
        {
            EventId = eventEntity.Id,
            RoleRequirementId = Guid.NewGuid(),
            Title = "Test Shift",
            StartTime = eventEntity.StartDate,
            EndTime = eventEntity.EndDate
        };
        var volunteer = new VolunteerProfile { UserId = Guid.NewGuid() };
        var assignment = new ShiftAssignment
        {
            ShiftId = shift.Id,
            VolunteerId = volunteer.Id,
            Status = "Confirmed",
            Shift = shift
        };

        db.Events.Add(eventEntity);
        db.Shifts.Add(shift);
        db.VolunteerProfiles.Add(volunteer);
        db.ShiftAssignments.Add(assignment);
        await db.SaveChangesAsync();

        return new Fixture(db, eventEntity, shift, volunteer, assignment);
    }

    private sealed class Fixture(
        Infrastructure.Data.AppDbContext db,
        Event eventEntity,
        Shift shift,
        VolunteerProfile volunteer,
        ShiftAssignment assignment) : IDisposable
    {
        public Infrastructure.Data.AppDbContext Db { get; } = db;
        public Event Event { get; } = eventEntity;
        public Shift Shift { get; } = shift;
        public VolunteerProfile Volunteer { get; } = volunteer;
        public ShiftAssignment Assignment { get; } = assignment;

        public CheckInRequestDto CheckIn(string token) => new()
        {
            VolunteerId = Volunteer.Id,
            EventId = Event.Id,
            ShiftId = Shift.Id,
            Token = token
        };

        public void Dispose() => Db.Dispose();
    }
}
