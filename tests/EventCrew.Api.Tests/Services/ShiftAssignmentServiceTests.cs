using EventCrew.Api.DTOs.Shifts;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using FluentAssertions;

namespace EventCrew.Api.Tests.Services;

public class ShiftAssignmentServiceTests
{
    private static (
        ShiftAssignmentService service,
        User organizer,
        User volunteerUser,
        VolunteerProfile volunteerProfile,
        Event ev,
        RoleRequirement role,
        Shift shift,
        AppDbContext db) SetupContext(int shiftCapacity = 2)
    {
        var db = TestDbFactory.Create();

        var organizer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Alex Organizer",
            Email = "alex@eventcrew.test",
            Role = "Organizer"
        };
        db.Users.Add(organizer);

        var volunteerUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Sam Volunteer",
            Email = "sam@eventcrew.test",
            Role = "Volunteer"
        };
        db.Users.Add(volunteerUser);

        var volunteerProfile = new VolunteerProfile
        {
            Id = Guid.NewGuid(),
            UserId = volunteerUser.Id,
            EmergencyContact = "0779998877",
            MaxHoursPerWeek = 25,
            RatingScore = 4.8m
        };
        db.VolunteerProfiles.Add(volunteerProfile);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizer.Id,
            Title = "Tech Expo 2026",
            Category = "Technology",
            StartDate = DateTimeOffset.UtcNow.AddDays(3),
            EndDate = DateTimeOffset.UtcNow.AddDays(4)
        };
        db.Events.Add(ev);

        var role = new RoleRequirement
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleName = "Information Desk",
            RequiredHeadcount = 5
        };
        db.RoleRequirements.Add(role);

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Morning Shift",
            StartTime = ev.StartDate.AddHours(9),
            EndTime = ev.StartDate.AddHours(13),
            Capacity = shiftCapacity,
            Status = "Scheduled"
        };
        db.Shifts.Add(shift);

        db.SaveChanges();

        var service = new ShiftAssignmentService(db);
        return (service, organizer, volunteerUser, volunteerProfile, ev, role, shift, db);
    }

    private static Application AddAcceptedApplication(AppDbContext db, Guid eventId, Guid volunteerId)
    {
        var app = new Application
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            VolunteerId = volunteerId,
            Status = "Accepted",
            AppliedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Applications.Add(app);
        db.SaveChanges();
        return app;
    }

    [Fact]
    public async Task AssignVolunteerAsync_WithValidAcceptedApplication_Succeeds()
    {
        var (service, organizer, _, volunteerProfile, ev, _, shift, db) = SetupContext();
        AddAcceptedApplication(db, ev.Id, volunteerProfile.Id);

        var dto = new AssignVolunteerDto
        {
            VolunteerId = volunteerProfile.Id,
            Status = "Confirmed"
        };

        var result = await service.AssignVolunteerAsync(shift.Id, dto, organizer.Id, "Organizer");

        result.Should().NotBeNull();
        result.ShiftId.Should().Be(shift.Id);
        result.VolunteerId.Should().Be(volunteerProfile.Id);
        result.VolunteerName.Should().Be("Sam Volunteer");
        result.Status.Should().Be("Confirmed");
        result.EventTitle.Should().Be("Tech Expo 2026");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WithNonexistentShift_ThrowsKeyNotFoundException()
    {
        var (service, organizer, _, volunteerProfile, _, _, _, _) = SetupContext();

        var dto = new AssignVolunteerDto
        {
            VolunteerId = volunteerProfile.Id
        };

        var act = async () => await service.AssignVolunteerAsync(Guid.NewGuid(), dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Shift not found*");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WithNonexistentVolunteer_ThrowsKeyNotFoundException()
    {
        var (service, organizer, _, _, _, _, shift, _) = SetupContext();

        var dto = new AssignVolunteerDto
        {
            VolunteerId = Guid.NewGuid()
        };

        var act = async () => await service.AssignVolunteerAsync(shift.Id, dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Volunteer profile not found*");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WithoutApplication_ThrowsInvalidOperationException()
    {
        var (service, organizer, _, volunteerProfile, _, _, shift, _) = SetupContext();

        var dto = new AssignVolunteerDto
        {
            VolunteerId = volunteerProfile.Id
        };

        var act = async () => await service.AssignVolunteerAsync(shift.Id, dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Volunteer is not eligible*Accepted or Assigned application*");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WithUnderReviewApplication_ThrowsInvalidOperationException()
    {
        var (service, organizer, _, volunteerProfile, ev, _, shift, db) = SetupContext();

        // Application exists but status is UnderReview
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            VolunteerId = volunteerProfile.Id,
            Status = "UnderReview",
            AppliedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        var dto = new AssignVolunteerDto
        {
            VolunteerId = volunteerProfile.Id
        };

        var act = async () => await service.AssignVolunteerAsync(shift.Id, dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Volunteer is not eligible*Accepted or Assigned application*");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WhenDuplicateAssignment_ThrowsInvalidOperationException()
    {
        var (service, organizer, _, volunteerProfile, ev, _, shift, db) = SetupContext();
        AddAcceptedApplication(db, ev.Id, volunteerProfile.Id);

        var dto = new AssignVolunteerDto
        {
            VolunteerId = volunteerProfile.Id
        };

        // First assignment succeeds
        await service.AssignVolunteerAsync(shift.Id, dto, organizer.Id, "Organizer");

        // Second assignment with same volunteer must fail
        var act = async () => await service.AssignVolunteerAsync(shift.Id, dto, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Volunteer is already assigned to this shift*");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WhenCapacityReached_ThrowsInvalidOperationException()
    {
        var (service, organizer, _, volunteer1, ev, _, shift, db) = SetupContext(shiftCapacity: 1);
        AddAcceptedApplication(db, ev.Id, volunteer1.Id);

        // Assign first volunteer to fill capacity
        await service.AssignVolunteerAsync(shift.Id, new AssignVolunteerDto { VolunteerId = volunteer1.Id }, organizer.Id, "Organizer");

        // Create second volunteer with accepted application
        var user2 = new User { Id = Guid.NewGuid(), FullName = "Second Volunteer", Email = "vol2@test.com" };
        var volunteer2 = new VolunteerProfile { Id = Guid.NewGuid(), UserId = user2.Id, EmergencyContact = "12345" };
        db.Users.Add(user2);
        db.VolunteerProfiles.Add(volunteer2);
        AddAcceptedApplication(db, ev.Id, volunteer2.Id);

        // Assigning second volunteer exceeds capacity
        var act = async () => await service.AssignVolunteerAsync(shift.Id, new AssignVolunteerDto { VolunteerId = volunteer2.Id }, organizer.Id, "Organizer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Shift has reached maximum capacity*");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WithUnauthorizedOrganizer_ThrowsUnauthorizedAccessException()
    {
        var (service, _, _, volunteerProfile, ev, _, shift, db) = SetupContext();
        AddAcceptedApplication(db, ev.Id, volunteerProfile.Id);

        var otherOrganizerId = Guid.NewGuid();

        var dto = new AssignVolunteerDto
        {
            VolunteerId = volunteerProfile.Id
        };

        var act = async () => await service.AssignVolunteerAsync(shift.Id, dto, otherOrganizerId, "Organizer");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WithVolunteerRole_ThrowsUnauthorizedAccessException()
    {
        var (service, _, _, volunteerProfile, ev, _, shift, db) = SetupContext();
        AddAcceptedApplication(db, ev.Id, volunteerProfile.Id);

        var dto = new AssignVolunteerDto
        {
            VolunteerId = volunteerProfile.Id
        };

        var act = async () => await service.AssignVolunteerAsync(shift.Id, dto, volunteerProfile.UserId, "Volunteer");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Volunteers are not authorized to assign volunteers*");
    }

    [Fact]
    public async Task AssignVolunteerAsync_WithAdminRole_BypassesOrganizerCheck()
    {
        var (service, _, _, volunteerProfile, ev, _, shift, db) = SetupContext();
        AddAcceptedApplication(db, ev.Id, volunteerProfile.Id);

        var adminId = Guid.NewGuid();

        var dto = new AssignVolunteerDto
        {
            VolunteerId = volunteerProfile.Id
        };

        var result = await service.AssignVolunteerAsync(shift.Id, dto, adminId, "Admin");

        result.Should().NotBeNull();
        result.VolunteerId.Should().Be(volunteerProfile.Id);
    }

    [Fact]
    public async Task GetMyAssignmentsAsync_ReturnsAssignmentsForVolunteer()
    {
        var (service, organizer, volunteerUser, volunteerProfile, ev, _, shift, db) = SetupContext();
        AddAcceptedApplication(db, ev.Id, volunteerProfile.Id);

        await service.AssignVolunteerAsync(shift.Id, new AssignVolunteerDto { VolunteerId = volunteerProfile.Id }, organizer.Id, "Organizer");

        // Query by VolunteerProfile.Id
        var myAssignments = await service.GetMyAssignmentsAsync(volunteerProfile.Id);
        myAssignments.Should().HaveCount(1);
        myAssignments[0].ShiftId.Should().Be(shift.Id);

        // Query by User.Id also works
        var myAssignmentsByUser = await service.GetMyAssignmentsAsync(volunteerUser.Id);
        myAssignmentsByUser.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveAssignmentAsync_RemovesAssignmentSuccessfully()
    {
        var (service, organizer, _, volunteerProfile, ev, _, shift, db) = SetupContext();
        AddAcceptedApplication(db, ev.Id, volunteerProfile.Id);

        var assigned = await service.AssignVolunteerAsync(shift.Id, new AssignVolunteerDto { VolunteerId = volunteerProfile.Id }, organizer.Id, "Organizer");

        var removed = await service.RemoveAssignmentAsync(shift.Id, assigned.Id, organizer.Id, "Organizer");
        removed.Should().BeTrue();

        var roster = await service.GetAssignmentsByShiftIdAsync(shift.Id, organizer.Id, "Organizer");
        roster.Should().BeEmpty();
    }
}
