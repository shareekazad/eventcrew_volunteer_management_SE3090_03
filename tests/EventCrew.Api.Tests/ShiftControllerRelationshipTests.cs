using EventCrew.Api.Controllers;
using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Tests;

public class ShiftControllerRelationshipTests
{
    [Fact]
    public async Task MyAssignmentsReturnsConfirmedAssignmentsForTheDemoVolunteer()
    {
        var options = new DbContextOptionsBuilder<EventCrewDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new EventCrewDbContext(options);
        var organizerId = Guid.NewGuid();
        var volunteer = new VolunteerProfile
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            FullName = "Volunteer",
            Email = "volunteer@test.com"
        };
        var otherVolunteer = new VolunteerProfile
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            FullName = "Other Volunteer",
            Email = "other@test.com"
        };
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerId,
            Title = "Community Festival",
            Category = "Community"
        };
        var roleRequirement = new RoleRequirement
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            RoleName = "Guest Services",
            RequiredHeadcount = 4
        };
        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            RoleRequirementId = roleRequirement.Id,
            Title = "Welcome Desk",
            StartTime = DateTimeOffset.Parse("2026-10-05T09:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-10-05T12:00:00Z"),
            Capacity = 4,
            Status = "Scheduled"
        };
        var ownAssignment = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shift.Id,
            VolunteerId = volunteer.Id,
            Status = "Confirmed"
        };
        var otherAssignment = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shift.Id,
            VolunteerId = otherVolunteer.Id,
            Status = "Confirmed"
        };
        context.VolunteerProfiles.AddRange(volunteer, otherVolunteer);
        context.Events.Add(eventEntity);
        context.RoleRequirements.Add(roleRequirement);
        context.Shifts.Add(shift);
        context.ShiftAssignments.AddRange(ownAssignment, otherAssignment);
        await context.SaveChangesAsync();

        var controller = new ShiftController(context);
        var result = await controller.GetMyAssignments(CancellationToken.None);

        var response = Assert.IsType<VolunteerAssignmentsResponse>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(volunteer.Id, response.VolunteerId);
        var assignment = Assert.Single(response.Assignments);
        Assert.Equal(ownAssignment.Id, assignment.AssignmentId);
        Assert.Equal(shift.Id, assignment.ShiftId);
        Assert.Equal("Guest Services", assignment.RoleRequirementName);
        Assert.Equal("Confirmed", assignment.Status);
    }

    [Fact]
    public async Task CreateRejectsRequirementOwnedByAnotherEvent()
    {
        var options = new DbContextOptionsBuilder<EventCrewDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new EventCrewDbContext(options);
        var organizerId = Guid.NewGuid();
        var selectedEvent = new Event { Id = Guid.NewGuid(), OrganizerId = organizerId, Title = "Selected event" };
        var otherEvent = new Event { Id = Guid.NewGuid(), OrganizerId = organizerId, Title = "Other event" };
        var requirement = new RoleRequirement
        {
            Id = Guid.NewGuid(),
            EventId = otherEvent.Id,
            Event = otherEvent,
            RoleName = "Registration",
            RequiredHeadcount = 4
        };
        context.Events.AddRange(selectedEvent, otherEvent);
        context.RoleRequirements.Add(requirement);
        await context.SaveChangesAsync();

        var controller = new ShiftController(context);
        var result = await controller.Create(new CreateShiftRequest
        {
            Title = "Morning registration",
            EventId = selectedEvent.Id,
            RoleRequirementId = requirement.Id,
            StartTime = DateTimeOffset.Parse("2026-10-10T08:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-10-10T12:00:00Z"),
            Capacity = 4
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var validation = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(
            "The role requirement must belong to the selected event.",
            validation.Errors[nameof(CreateShiftRequest.RoleRequirementId)]);
        Assert.Empty(await context.Shifts.ToListAsync());
    }
}
