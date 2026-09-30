using EventCrew.Api.DTOs.Venues;
using EventCrew.Api.Services;
using FluentAssertions;

namespace EventCrew.Api.Tests.Services;

public class VenueServiceTests
{
    // ----------------------------------------------------------
    // GetAllAsync
    // ----------------------------------------------------------
    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoVenuesExist()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var service = new VenueService(db);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ----------------------------------------------------------
    // CreateAsync
    // ----------------------------------------------------------
    [Fact]
    public async Task CreateAsync_PersistsVenue_AndReturnsDto()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var service = new VenueService(db);

        var dto = new CreateVenueDto
        {
            Name = "BMICH",
            Address = "Bauddhaloka Mawatha",
            City = "Colombo",
            Capacity = 2500,
            Latitude = 6.9012m,
            Longitude = 79.8626m
        };

        // Act
        var created = await service.CreateAsync(dto);

        // Assert
        created.Id.Should().NotBeEmpty();
        created.Name.Should().Be("BMICH");
        created.City.Should().Be("Colombo");
        created.Capacity.Should().Be(2500);

        db.Venues.Should().HaveCount(1);
        db.Venues.Single().Name.Should().Be("BMICH");
    }

    // ----------------------------------------------------------
    // GetByIdAsync
    // ----------------------------------------------------------
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = TestDbFactory.Create();
        var service = new VenueService(db);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsVenue_WhenFound()
    {
        using var db = TestDbFactory.Create();
        var service = new VenueService(db);

        var created = await service.CreateAsync(new CreateVenueDto
        {
            Name = "Test Venue",
            Address = "Test Address",
            City = "Test City",
            Capacity = 100
        });

        var fetched = await service.GetByIdAsync(created.Id);

        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Test Venue");
    }

    // ----------------------------------------------------------
    // UpdateAsync
    // ----------------------------------------------------------
    [Fact]
    public async Task UpdateAsync_UpdatesFields_AndTimestamp()
    {
        using var db = TestDbFactory.Create();
        var service = new VenueService(db);

        var created = await service.CreateAsync(new CreateVenueDto
        {
            Name = "Old Name",
            Address = "Old Address",
            City = "Old City",
            Capacity = 100
        });

        // small delay so UpdatedAt definitely advances
        await Task.Delay(10);

        var updated = await service.UpdateAsync(created.Id, new UpdateVenueDto
        {
            Name = "New Name",
            Address = "New Address",
            City = "New City",
            Capacity = 200
        });

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name");
        updated.Capacity.Should().Be(200);
        updated.UpdatedAt.Should().BeAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        using var db = TestDbFactory.Create();
        var service = new VenueService(db);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateVenueDto
        {
            Name = "X",
            Address = "Y",
            City = "Z",
            Capacity = 1
        });

        result.Should().BeNull();
    }

    // ----------------------------------------------------------
    // DeleteAsync
    // ----------------------------------------------------------
    [Fact]
    public async Task DeleteAsync_RemovesVenue_WhenExists()
    {
        using var db = TestDbFactory.Create();
        var service = new VenueService(db);

        var created = await service.CreateAsync(new CreateVenueDto
        {
            Name = "Delete Me",
            Address = "A",
            City = "B",
            Capacity = 5
        });

        var deleted = await service.DeleteAsync(created.Id);

        deleted.Should().BeTrue();
        db.Venues.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = TestDbFactory.Create();
        var service = new VenueService(db);

        var result = await service.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }
}