using EventCrew.Api.Controllers;
using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EventCrew.Api.Tests;

public class AssignmentControllerTests
{
    [Fact]
    public async Task CreateAssignsVolunteerWithEligibleApplication()
    {
        await using var context = CreateContext();
        var data = await SeedShiftAsync(context, capacity: 2);
        var volunteer = await AddVolunteerAsync(context, data, "Accepted", data.Requirement.Id);

        var result = await AssignmentControllerFor(context, data).Create(
            new CreateShiftAssignmentRequest { ShiftId = data.Shift.Id, VolunteerId = volunteer.Id },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ShiftAssignmentResponse>(created.Value);
        Assert.Equal(data.Shift.Id, response.ShiftId);
        Assert.Equal(volunteer.Id, response.VolunteerId);
        Assert.Equal("Confirmed", response.Status);
        Assert.Single(await context.ShiftAssignments.ToListAsync());
    }

    [Fact]
    public async Task CreateRejectsDuplicateVolunteerAndShiftPair()
    {
        await using var context = CreateContext();
        var data = await SeedShiftAsync(context, capacity: 3);
        var volunteer = await AddVolunteerAsync(context, data, "Accepted", data.Requirement.Id);
        context.ShiftAssignments.Add(CreateAssignment(data.Shift.Id, volunteer.Id));
        await context.SaveChangesAsync();

        var result = await AssignmentControllerFor(context, data).Create(
            new CreateShiftAssignmentRequest { ShiftId = data.Shift.Id, VolunteerId = volunteer.Id },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("already assigned", Assert.IsType<ProblemDetails>(conflict.Value).Detail);
        Assert.Single(await context.ShiftAssignments.ToListAsync());
    }

    [Fact]
    public async Task CreateRejectsAssignmentWhenShiftIsFull()
    {
        await using var context = CreateContext();
        var data = await SeedShiftAsync(context, capacity: 1);
        var assigned = await AddVolunteerAsync(context, data, "Accepted", data.Requirement.Id);
        var candidate = await AddVolunteerAsync(context, data, "Accepted", data.Requirement.Id);
        context.ShiftAssignments.Add(CreateAssignment(data.Shift.Id, assigned.Id));
        await context.SaveChangesAsync();

        var result = await AssignmentControllerFor(context, data).Create(
            new CreateShiftAssignmentRequest { ShiftId = data.Shift.Id, VolunteerId = candidate.Id },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("capacity", Assert.IsType<ProblemDetails>(conflict.Value).Detail);
        Assert.Single(await context.ShiftAssignments.ToListAsync());
    }

    [Fact]
    public async Task CreateRejectsVolunteerWithoutAcceptedApplicationForShiftRole()
    {
        await using var context = CreateContext();
        var data = await SeedShiftAsync(context, capacity: 2);
        var volunteer = await AddVolunteerAsync(context, data, "Rejected", data.Requirement.Id);

        var result = await AssignmentControllerFor(context, data).Create(
            new CreateShiftAssignmentRequest { ShiftId = data.Shift.Id, VolunteerId = volunteer.Id },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(await context.ShiftAssignments.ToListAsync());
    }

    [Fact]
    public async Task EligibleVolunteersOnlyIncludesMatchingAcceptedApplications()
    {
        await using var context = CreateContext();
        var data = await SeedShiftAsync(context, capacity: 3);
        var eligible = await AddVolunteerAsync(context, data, "Accepted", data.Requirement.Id);
        await AddVolunteerAsync(context, data, "Accepted", Guid.NewGuid());
        await AddVolunteerAsync(context, data, "Submitted", data.Requirement.Id);

        var result = await AssignmentControllerFor(context, data).GetEligibleVolunteers(data.Shift.Id, CancellationToken.None);

        var candidates = Assert.IsType<OkObjectResult>(result.Result).Value as IReadOnlyList<EligibleVolunteerResponse>;
        Assert.NotNull(candidates);
        var candidate = Assert.Single(candidates);
        Assert.Equal(eligible.Id, candidate.Id);
    }

    [Fact]
    public async Task ShiftResponseIncludesAssignmentCapacitySummary()
    {
        await using var context = CreateContext();
        var data = await SeedShiftAsync(context, capacity: 2);
        var volunteer = await AddVolunteerAsync(context, data, "Accepted", data.Requirement.Id);
        context.ShiftAssignments.Add(CreateAssignment(data.Shift.Id, volunteer.Id));
        await context.SaveChangesAsync();

        var result = await ControllerTestAuth.AsUser(new ShiftController(context), data.Event.OrganizerId, "Organizer").GetAll(CancellationToken.None);

        var response = Assert.Single(Assert.IsType<OkObjectResult>(result.Result).Value as IReadOnlyList<ShiftResponse> ?? []);
        Assert.Equal(1, response.AssignedCount);
        Assert.Equal(1, response.RemainingCapacity);
    }

    private static EventCrewDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventCrewDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new EventCrewDbContext(options);
    }

    private static async Task<ShiftTestData> SeedShiftAsync(EventCrewDbContext context, int capacity)
    {
        var eventEntity = new Event { Id = Guid.NewGuid(), OrganizerId = Guid.NewGuid(), Title = "Tech conference" };
        var requirement = new RoleRequirement
        {
            Id = Guid.NewGuid(), EventId = eventEntity.Id, Event = eventEntity,
            RoleName = "Registration", RequiredHeadcount = capacity
        };
        var shift = new Shift
        {
            Id = Guid.NewGuid(), EventId = eventEntity.Id, RoleRequirementId = requirement.Id,
            Event = eventEntity, RoleRequirement = requirement, Title = "Morning registration",
            StartTime = DateTimeOffset.Parse("2026-10-10T08:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-10-10T12:00:00Z"), Capacity = capacity,
            Status = "Scheduled", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Events.Add(eventEntity);
        context.RoleRequirements.Add(requirement);
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();
        return new ShiftTestData(eventEntity, requirement, shift);
    }

    private static async Task<VolunteerProfile> AddVolunteerAsync(
        EventCrewDbContext context,
        ShiftTestData data,
        string applicationStatus,
        Guid? applicationRequirementId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), FullName = $"Volunteer {Guid.NewGuid():N}"[..20],
            Email = $"{Guid.NewGuid():N}@example.test", Role = "Volunteer", IsActive = true
        };
        var profile = new VolunteerProfile { Id = Guid.NewGuid(), UserId = user.Id, User = user };
        context.Users.Add(user);
        context.VolunteerProfiles.Add(profile);
        context.Applications.Add(new Application
        {
            Id = Guid.NewGuid(), EventId = data.Event.Id, VolunteerProfileId = profile.Id,
            RoleRequirementId = applicationRequirementId, Status = applicationStatus
        });
        await context.SaveChangesAsync();
        return profile;
    }

    private static ShiftAssignment CreateAssignment(Guid shiftId, Guid volunteerId)
    {
        var now = DateTimeOffset.UtcNow;
        return new ShiftAssignment
        {
            Id = Guid.NewGuid(), ShiftId = shiftId, VolunteerId = volunteerId,
            Status = "Confirmed", AssignedAt = now, CreatedAt = now, UpdatedAt = now
        };
    }

    private static AssignmentController AssignmentControllerFor(EventCrewDbContext context, ShiftTestData data) =>
        ControllerTestAuth.AsUser(new AssignmentController(context), data.Event.OrganizerId, "Organizer");

    private sealed record ShiftTestData(Event Event, RoleRequirement Requirement, Shift Shift);
}
