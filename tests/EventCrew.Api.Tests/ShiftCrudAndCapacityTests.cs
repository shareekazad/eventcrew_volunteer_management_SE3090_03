using EventCrew.Api.Controllers;
using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Tests;

public class ShiftCrudAndCapacityTests
{
    [Fact]
    public async Task UpdateRejectsCapacityReductionBelowExistingAssignments()
    {
        var options = new DbContextOptionsBuilder<EventCrewDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new EventCrewDbContext(options);

        var organizerId = Guid.NewGuid();
        var ev = new Event { Id = Guid.NewGuid(), OrganizerId = organizerId, Title = "Tech Conference" };
        var req = new RoleRequirement { Id = Guid.NewGuid(), EventId = ev.Id, RoleName = "Guide", RequiredHeadcount = 10 };
        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleRequirementId = req.Id,
            Title = "Morning Shift",
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            EndTime = DateTimeOffset.UtcNow.AddHours(5),
            Capacity = 5,
            Status = "Scheduled"
        };
        context.Events.Add(ev);
        context.RoleRequirements.Add(req);
        context.Shifts.Add(shift);

        // Add 4 existing confirmed assignments
        for (int i = 0; i < 4; i++)
        {
            var user = new User { Id = Guid.NewGuid(), FullName = $"Volunteer {i}", Email = $"vol{i}@test.com", Role = "Volunteer" };
            var profile = new VolunteerProfile { Id = Guid.NewGuid(), UserId = user.Id, User = user };
            var assignment = new ShiftAssignment { Id = Guid.NewGuid(), ShiftId = shift.Id, VolunteerId = profile.Id, Status = "Confirmed" };
            context.Users.Add(user);
            context.VolunteerProfiles.Add(profile);
            context.ShiftAssignments.Add(assignment);
        }
        await context.SaveChangesAsync();

        var controller = ControllerTestAuth.AsUser(new ShiftController(context), organizerId, "Organizer");

        // Try updating capacity to 3 (less than 4 existing assignments) -> Reject
        var updateRequest3 = new UpdateShiftRequest
        {
            Title = "Morning Shift Updated",
            EventId = ev.Id,
            RoleRequirementId = req.Id,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Capacity = 3
        };
        var result3 = await controller.Update(shift.Id, updateRequest3, CancellationToken.None);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result3);
        var validation = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains("Capacity cannot be reduced below the number of existing assignments", validation.Errors[nameof(UpdateShiftRequest.Capacity)][0]);

        // Update capacity to 4 (equal to 4 existing assignments) -> Allow
        var updateRequest4 = updateRequest3 with { Capacity = 4 };
        var result4 = await controller.Update(shift.Id, updateRequest4, CancellationToken.None);
        Assert.IsType<NoContentResult>(result4);

        // Update capacity to 5 (greater than 4 existing assignments) -> Allow
        var updateRequest5 = updateRequest3 with { Capacity = 5 };
        var result5 = await controller.Update(shift.Id, updateRequest5, CancellationToken.None);
        Assert.IsType<NoContentResult>(result5);
    }

    [Fact]
    public async Task ShiftCrudLifecycleWorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<EventCrewDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new EventCrewDbContext(options);

        var organizerId = Guid.NewGuid();
        var ev = new Event { Id = Guid.NewGuid(), OrganizerId = organizerId, Title = "Music Fest" };
        var req = new RoleRequirement { Id = Guid.NewGuid(), EventId = ev.Id, RoleName = "Stage Crew", RequiredHeadcount = 5 };
        context.Events.Add(ev);
        context.RoleRequirements.Add(req);
        await context.SaveChangesAsync();

        var controller = ControllerTestAuth.AsUser(new ShiftController(context), organizerId, "Organizer");

        // Create
        var createRequest = new CreateShiftRequest
        {
            Title = "Main Stage Setup",
            EventId = ev.Id,
            RoleRequirementId = req.Id,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(4),
            Capacity = 6
        };
        var createResult = await controller.Create(createRequest, CancellationToken.None);
        var createdAction = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdShift = Assert.IsType<ShiftResponse>(createdAction.Value);
        Assert.Equal("Main Stage Setup", createdShift.Title);
        Assert.Equal(6, createdShift.Capacity);

        // Read List
        var listResult = await controller.GetAll(CancellationToken.None);
        var okList = Assert.IsType<OkObjectResult>(listResult.Result);
        var shiftsList = Assert.IsAssignableFrom<IEnumerable<ShiftResponse>>(okList.Value);
        Assert.Single(shiftsList);

        // Read By ID
        var getByIdResult = await controller.GetById(createdShift.Id, CancellationToken.None);
        var okItem = Assert.IsType<OkObjectResult>(getByIdResult.Result);
        var item = Assert.IsType<ShiftResponse>(okItem.Value);
        Assert.Equal(createdShift.Id, item.Id);

        // Delete
        var deleteResult = await controller.Delete(createdShift.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);

        // Verify NotFound after delete
        var getAfterDelete = await controller.GetById(createdShift.Id, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(getAfterDelete.Result);
    }
}
