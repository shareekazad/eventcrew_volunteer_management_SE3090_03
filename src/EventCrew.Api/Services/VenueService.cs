using EventCrew.Api.DTOs.Venues;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

/// <summary>
/// Business logic for Venue operations. Talks to AppDbContext and maps to/from DTOs.
/// Controllers delegate to this class — the controller stays HTTP-thin.
/// </summary>
public class VenueService : IVenueService
{
    private readonly AppDbContext _db;

    public VenueService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<VenueResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var venues = await _db.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

        return venues.Select(MapToDto).ToList();
    }

    public async Task<VenueResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var venue = await _db.Venues
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        return venue is null ? null : MapToDto(venue);
    }

    public async Task<VenueResponseDto> CreateAsync(CreateVenueDto dto, CancellationToken cancellationToken = default)
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Capacity = dto.Capacity,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.Venues.Add(venue);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(venue);
    }

    public async Task<VenueResponseDto?> UpdateAsync(Guid id, UpdateVenueDto dto, CancellationToken cancellationToken = default)
    {
        var venue = await _db.Venues
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (venue is null)
            return null;

        venue.Name = dto.Name.Trim();
        venue.Address = dto.Address.Trim();
        venue.City = dto.City.Trim();
        venue.Latitude = dto.Latitude;
        venue.Longitude = dto.Longitude;
        venue.Capacity = dto.Capacity;
        venue.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(venue);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var venue = await _db.Venues
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (venue is null)
            return false;

        _db.Venues.Remove(venue);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ---- private helpers ----
    private static VenueResponseDto MapToDto(Venue v) => new()
    {
        Id = v.Id,
        Name = v.Name,
        Address = v.Address,
        City = v.City,
        Latitude = v.Latitude,
        Longitude = v.Longitude,
        Capacity = v.Capacity,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };
}