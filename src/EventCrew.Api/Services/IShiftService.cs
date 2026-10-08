using EventCrew.Api.DTOs.Shifts;

namespace EventCrew.Api.Services;

public interface IShiftService
{
    Task<IReadOnlyList<ShiftResponseDto>> GetAllAsync(
        Guid? eventId = null,
        DateTimeOffset? date = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<ShiftResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ShiftResponseDto> CreateAsync(
        CreateShiftDto dto,
        Guid? userId = null,
        string? userRole = null,
        CancellationToken cancellationToken = default);

    Task<ShiftResponseDto?> UpdateAsync(
        Guid id,
        UpdateShiftDto dto,
        Guid? userId = null,
        string? userRole = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid? userId = null,
        string? userRole = null,
        CancellationToken cancellationToken = default);
}
