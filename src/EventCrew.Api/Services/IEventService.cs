using EventCrew.Api.DTOs.Events;

namespace EventCrew.Api.Services;

/// <summary>
/// Contract for event business logic, including nested role requirements
/// and controlled status transitions.
/// </summary>
public interface IEventService
{
    // ---- Event CRUD ----
    Task<IReadOnlyList<EventResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventResponseDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);
    Task<EventResponseDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // ---- Status transition ----
    /// <summary>
    /// Attempts a status transition on an event.
    /// Returns the updated event on success, null if not found.
    /// Throws InvalidOperationException for illegal transitions.
    /// </summary>
    Task<EventResponseDto?> UpdateStatusAsync(Guid id, UpdateEventStatusDto dto, CancellationToken cancellationToken = default);

    // ---- Nested role requirements ----
    Task<RoleRequirementDto?> AddRoleAsync(Guid eventId, RoleRequirementInputDto dto, CancellationToken cancellationToken = default);
    Task<bool> RemoveRoleAsync(Guid eventId, Guid roleId, CancellationToken cancellationToken = default);
}