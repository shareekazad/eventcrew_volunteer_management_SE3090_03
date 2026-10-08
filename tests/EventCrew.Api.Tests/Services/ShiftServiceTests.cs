using EventCrew.Api.DTOs.Shifts;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using FluentAssertions;

namespace EventCrew.Api.Tests.Services;

public class ShiftServiceTests
{
    private static (ShiftService service, User organizer, Event ev, RoleRequirement role, AppDbContext db) SetupContext()
    {
        var db = TestDbFactory.Create();

        var organizer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Organizer",
            Email = "jane@eventcrew.test",
            Role = "Organizer"
        };
        db.Users.Add(organizer);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizer.Id,
            Title = "Music Festival",
            Category = "Music",
            StartDate = DateTimeOffset.UtcNow.AddDays(5),
            EndDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        db.Events.Add(ev);

        var role = new RoleRequirement
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleName = "Stage Hand",
            RequiredHeadcount = 10
        };
        db.RoleRequirements.Add(role);

        db.SaveChanges();

        var service = new ShiftService(db);
        return (service, organizer, ev, role, db);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesShiftSuccessfully()
    {
        var (service, organizer, ev, role, _) = SetupContext();

        var dto = new CreateShiftDto
        {
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Morning Stage Setup",
            Description = "Prepare cables and microphones",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 5,
            Status = "Scheduled"
        };

        var result = await service.CreateAsync(dto, organizer.Id, "Organizer");

        result.Should().NotBeNull();
        result.Title.Should().Be("Morning Stage Setup");
        result.Capacity.Should().Be(5);
        result.AssignedCount.Should().Be(0);
        result.RemainingCapacity.Should().Be(5);
        result.EventId.Should().Be(ev.Id);
        result.RoleRequirementId.Should().Be(role.Id);
        result.RoleName.Should().Be("Stage Hand");
    }

    [Fact]
    public async Task CreateAsync_WithEndTimeBeforeStartTime_ThrowsInvalidOperationException()
    {
        var (service, organizer, ev, role, _) = SetupContext();

        var dto = new CreateShiftDto
        {
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Invalid Time Shift",
            StartTime = ev.StartDate.AddHours(12),
            EndTime = ev.StartDate.AddHours(8),
            Capacity = 3
        };

        var act = async () => await service.CreateAsync(dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*End time must be after start time*");
    }

    [Fact]
    public async Task CreateAsync_WithZeroOrNegativeCapacity_ThrowsInvalidOperationException()
    {
        var (service, organizer, ev, role, _) = SetupContext();

        var dto = new CreateShiftDto
        {
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Zero Capacity Shift",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 0
        };

        var act = async () => await service.CreateAsync(dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Capacity must be greater than zero*");
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentEvent_ThrowsInvalidOperationException()
    {
        var (service, organizer, _, role, _) = SetupContext();

        var dto = new CreateShiftDto
        {
            EventId = Guid.NewGuid(),
            RoleRequirementId = role.Id,
            Title = "Ghost Event Shift",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(4),
            Capacity = 2
        };

        var act = async () => await service.CreateAsync(dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Event does not exist*");
    }

    [Fact]
    public async Task CreateAsync_WithRoleRequirementMismatch_ThrowsInvalidOperationException()
    {
        var (service, organizer, ev, _, db) = SetupContext();

        // Create another event and role requirement
        var otherEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizer.Id,
            Title = "Other Event",
            Category = "Art",
            StartDate = DateTimeOffset.UtcNow.AddDays(10),
            EndDate = DateTimeOffset.UtcNow.AddDays(12)
        };
        db.Events.Add(otherEvent);

        var otherRole = new RoleRequirement
        {
            Id = Guid.NewGuid(),
            EventId = otherEvent.Id,
            RoleName = "Ticket Checker",
            RequiredHeadcount = 2
        };
        db.RoleRequirements.Add(otherRole);
        db.SaveChanges();

        var dto = new CreateShiftDto
        {
            EventId = ev.Id,
            RoleRequirementId = otherRole.Id, // from another event!
            Title = "Mismatched Role Shift",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 4
        };

        var act = async () => await service.CreateAsync(dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Role requirement does not belong to the specified event*");
    }

    [Fact]
    public async Task CreateAsync_WithUnauthorizedOrganizer_ThrowsUnauthorizedAccessException()
    {
        var (service, _, ev, role, _) = SetupContext();

        var anotherUserId = Guid.NewGuid();

        var dto = new CreateShiftDto
        {
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Unauthorized Shift",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 5
        };

        var act = async () => await service.CreateAsync(dto, anotherUserId, "Organizer");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task CreateAsync_WithAdminRole_BypassesOrganizerCheck()
    {
        var (service, _, ev, role, _) = SetupContext();

        var adminId = Guid.NewGuid();

        var dto = new CreateShiftDto
        {
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Admin Created Shift",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 5
        };

        var result = await service.CreateAsync(dto, adminId, "Admin");

        result.Should().NotBeNull();
        result.Title.Should().Be("Admin Created Shift");
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesShift()
    {
        var (service, organizer, ev, role, _) = SetupContext();

        var created = await service.CreateAsync(new CreateShiftDto
        {
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Initial Title",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 5
        }, organizer.Id, "Organizer");

        var updateDto = new UpdateShiftDto
        {
            Title = "Updated Shift Title",
            Description = "Updated description",
            StartTime = ev.StartDate.AddHours(9),
            EndTime = ev.StartDate.AddHours(13),
            Capacity = 8,
            Status = "InProgress"
        };

        var updated = await service.UpdateAsync(created.Id, updateDto, organizer.Id, "Organizer");

        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated Shift Title");
        updated.Description.Should().Be("Updated description");
        updated.Capacity.Should().Be(8);
        updated.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task UpdateAsync_WhenReducingCapacityBelowCurrentAssignments_ThrowsInvalidOperationException()
    {
        var (service, organizer, ev, role, db) = SetupContext();

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Crowded Shift",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 5
        };
        db.Shifts.Add(shift);

        // Add 3 active assignments
        for (int i = 0; i < 3; i++)
        {
            shift.Assignments.Add(new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                ShiftId = shift.Id,
                VolunteerId = Guid.NewGuid(),
                Status = "Confirmed"
            });
        }
        db.SaveChanges();

        var updateDto = new UpdateShiftDto
        {
            Title = "Crowded Shift",
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Capacity = 2 // less than 3 active assignments!
        };

        var act = async () => await service.UpdateAsync(shift.Id, updateDto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Capacity cannot be reduced below the current number of assignments*");
    }

    [Fact]
    public async Task DeleteAsync_WhenShiftExists_RemovesShift()
    {
        var (service, organizer, ev, role, _) = SetupContext();

        var created = await service.CreateAsync(new CreateShiftDto
        {
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Shift to Delete",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 4
        }, organizer.Id, "Organizer");

        var deleted = await service.DeleteAsync(created.Id, organizer.Id, "Organizer");
        deleted.Should().BeTrue();

        var fetched = await service.GetByIdAsync(created.Id);
        fetched.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_FiltersByEventAndStatus()
    {
        var (service, organizer, ev, role, db) = SetupContext();

        var shift1 = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Shift A",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 4,
            Status = "Scheduled"
        };
        var shift2 = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Shift B",
            StartTime = ev.StartDate.AddHours(13),
            EndTime = ev.StartDate.AddHours(17),
            Capacity = 4,
            Status = "Cancelled"
        };
        db.Shifts.AddRange(shift1, shift2);
        db.SaveChanges();

        var scheduledShifts = await service.GetAllAsync(ev.Id, status: "Scheduled");
        scheduledShifts.Should().HaveCount(1);
        scheduledShifts[0].Title.Should().Be("Shift A");

        var allForEvent = await service.GetAllAsync(ev.Id);
        allForEvent.Should().HaveCount(2);
    }
}
