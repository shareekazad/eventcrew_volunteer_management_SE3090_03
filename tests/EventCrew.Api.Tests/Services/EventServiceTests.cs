using EventCrew.Api.DTOs.Events;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using FluentAssertions;

namespace EventCrew.Api.Tests.Services;

public class EventServiceTests
{
    // ============================================================
    // Helpers
    // ============================================================
    private static async Task<User> AddTestOrganizerAsync(Infrastructure.Data.AppDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test Organizer",
            Email = $"org-{Guid.NewGuid()}@test.local",
            Role = "Organizer"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Venue> AddTestVenueAsync(Infrastructure.Data.AppDbContext db)
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = "Test Venue",
            Address = "123 Test",
            City = "Colombo",
            Capacity = 500
        };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        return venue;
    }

    // ============================================================
    // Create — success
    // ============================================================
    [Fact]
    public async Task CreateAsync_CreatesDraftEvent_WithNestedRoles()
    {
        using var db = TestDbFactory.Create();
        var organizer = await AddTestOrganizerAsync(db);
        var venue = await AddTestVenueAsync(db);
        var service = new EventService(db);

        var dto = new CreateEventDto
        {
            OrganizerId = organizer.Id,
            VenueId = venue.Id,
            Title = "Tech Meetup",
            Category = "Conference",
            StartDate = DateTimeOffset.UtcNow.AddDays(10),
            EndDate = DateTimeOffset.UtcNow.AddDays(10).AddHours(8),
            RoleRequirements = new List<RoleRequirementInputDto>
            {
                new() { RoleName = "Usher", RequiredHeadcount = 5, MinExperienceLevel = "Beginner" },
                new() { RoleName = "Registration", RequiredHeadcount = 2, MinExperienceLevel = "Intermediate" }
            }
        };

        var result = await service.CreateAsync(dto);

        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("Tech Meetup");
        result.Status.Should().Be("Draft");
        result.RoleRequirements.Should().HaveCount(2);
        result.RoleRequirements.Should().Contain(r => r.RoleName == "Usher" && r.RequiredHeadcount == 5);
        result.RoleRequirements.Should().Contain(r => r.RoleName == "Registration" && r.RequiredHeadcount == 2);
    }

    // ============================================================
    // Business rule: start < end
    // ============================================================
    [Fact]
    public async Task CreateAsync_Throws_WhenStartDateAfterEndDate()
    {
        using var db = TestDbFactory.Create();
        var organizer = await AddTestOrganizerAsync(db);
        var service = new EventService(db);

        var dto = new CreateEventDto
        {
            OrganizerId = organizer.Id,
            Title = "Bad Dates",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow.AddDays(10),
            EndDate = DateTimeOffset.UtcNow.AddDays(5)
        };

        var act = async () => await service.CreateAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Start date must be before end date*");
    }

    // ============================================================
    // Business rule: organizer must exist
    // ============================================================
    [Fact]
    public async Task CreateAsync_Throws_WhenOrganizerDoesNotExist()
    {
        using var db = TestDbFactory.Create();
        var service = new EventService(db);

        var dto = new CreateEventDto
        {
            OrganizerId = Guid.NewGuid(),
            Title = "Ghost Organizer",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow.AddDays(1),
            EndDate = DateTimeOffset.UtcNow.AddDays(2)
        };

        var act = async () => await service.CreateAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Organizer does not exist*");
    }

    // ============================================================
    // Business rule: venue must exist if provided
    // ============================================================
    [Fact]
    public async Task CreateAsync_Throws_WhenVenueDoesNotExist()
    {
        using var db = TestDbFactory.Create();
        var organizer = await AddTestOrganizerAsync(db);
        var service = new EventService(db);

        var dto = new CreateEventDto
        {
            OrganizerId = organizer.Id,
            VenueId = Guid.NewGuid(),
            Title = "Ghost Venue",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow.AddDays(1),
            EndDate = DateTimeOffset.UtcNow.AddDays(2)
        };

        var act = async () => await service.CreateAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Venue does not exist*");
    }

    // ============================================================
    // Status transitions — legal
    // ============================================================
    [Fact]
    public async Task UpdateStatusAsync_AllowsLegalTransition_DraftToPublished()
    {
        using var db = TestDbFactory.Create();
        var organizer = await AddTestOrganizerAsync(db);
        var service = new EventService(db);

        var created = await service.CreateAsync(new CreateEventDto
        {
            OrganizerId = organizer.Id,
            Title = "Test Event",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow.AddDays(1),
            EndDate = DateTimeOffset.UtcNow.AddDays(2)
        });

        var result = await service.UpdateStatusAsync(created.Id,
            new UpdateEventStatusDto { NewStatus = "Published" });

        result.Should().NotBeNull();
        result!.Status.Should().Be("Published");
    }

    // ============================================================
    // Status transitions — illegal
    // ============================================================
    [Fact]
    public async Task UpdateStatusAsync_Throws_OnIllegalTransition_DraftToCompleted()
    {
        using var db = TestDbFactory.Create();
        var organizer = await AddTestOrganizerAsync(db);
        var service = new EventService(db);

        var created = await service.CreateAsync(new CreateEventDto
        {
            OrganizerId = organizer.Id,
            Title = "Test",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow.AddDays(1),
            EndDate = DateTimeOffset.UtcNow.AddDays(2)
        });

        var act = async () => await service.UpdateStatusAsync(created.Id,
            new UpdateEventStatusDto { NewStatus = "Completed" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition from 'Draft' to 'Completed'*");
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsNull_WhenEventNotFound()
    {
        using var db = TestDbFactory.Create();
        var service = new EventService(db);

        var result = await service.UpdateStatusAsync(Guid.NewGuid(),
            new UpdateEventStatusDto { NewStatus = "Published" });

        result.Should().BeNull();
    }

    // ============================================================
    // Nested role — AddRoleAsync
    // ============================================================
    [Fact]
    public async Task AddRoleAsync_AddsRole_ToDraftEvent()
    {
        using var db = TestDbFactory.Create();
        var organizer = await AddTestOrganizerAsync(db);
        var service = new EventService(db);

        var created = await service.CreateAsync(new CreateEventDto
        {
            OrganizerId = organizer.Id,
            Title = "Test",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow.AddDays(1),
            EndDate = DateTimeOffset.UtcNow.AddDays(2)
        });

        var role = await service.AddRoleAsync(created.Id, new RoleRequirementInputDto
        {
            RoleName = "Photographer",
            RequiredHeadcount = 1,
            MinExperienceLevel = "Advanced"
        });

        role.Should().NotBeNull();
        role!.RoleName.Should().Be("Photographer");
    }

    [Fact]
    public async Task AddRoleAsync_Throws_WhenEventIsCancelled()
    {
        using var db = TestDbFactory.Create();
        var organizer = await AddTestOrganizerAsync(db);
        var service = new EventService(db);

        var created = await service.CreateAsync(new CreateEventDto
        {
            OrganizerId = organizer.Id,
            Title = "Test",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow.AddDays(1),
            EndDate = DateTimeOffset.UtcNow.AddDays(2)
        });

        await service.UpdateStatusAsync(created.Id, new UpdateEventStatusDto { NewStatus = "Cancelled" });

        var act = async () => await service.AddRoleAsync(created.Id, new RoleRequirementInputDto
        {
            RoleName = "Late Addition",
            RequiredHeadcount = 1,
            MinExperienceLevel = "Beginner"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot add role requirements*Cancelled*");
    }

    // ============================================================
    // Delete
    // ============================================================
    [Fact]
    public async Task DeleteAsync_RemovesEvent()
    {
        using var db = TestDbFactory.Create();
        var organizer = await AddTestOrganizerAsync(db);
        var service = new EventService(db);

        var created = await service.CreateAsync(new CreateEventDto
        {
            OrganizerId = organizer.Id,
            Title = "Delete Me",
            Category = "Test",
            StartDate = DateTimeOffset.UtcNow.AddDays(1),
            EndDate = DateTimeOffset.UtcNow.AddDays(2)
        });

        var deleted = await service.DeleteAsync(created.Id);

        deleted.Should().BeTrue();
        db.Events.Should().BeEmpty();
    }
}