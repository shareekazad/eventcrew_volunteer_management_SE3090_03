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

        var controller = ControllerTestAuth.AsUser(new ShiftController(context), organizerId, "Organizer");
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
