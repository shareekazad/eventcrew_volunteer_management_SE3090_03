using EventCrew.Api.DTOs.Venues;

namespace EventCrew.Api.Services;

/// <summary>
/// Contract for venue business logic.
/// The interface lets us swap implementations (real, mocked, in-memory) for tests.
/// </summary>
public interface IVenueService
{
    Task<IReadOnlyList<VenueResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VenueResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VenueResponseDto> CreateAsync(CreateVenueDto dto, CancellationToken cancellationToken = default);
    Task<VenueResponseDto?> UpdateAsync(Guid id, UpdateVenueDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}