using EventCrew.Api.DTOs.Shifts;

namespace EventCrew.Api.Services;

public interface IShiftAssignmentService
{
    Task<ShiftAssignmentResponseDto> AssignVolunteerAsync(
        Guid shiftId,
        AssignVolunteerDto dto,
        Guid? actingUserId = null,
        string? actingUserRole = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftAssignmentResponseDto>> GetAssignmentsByShiftIdAsync(
        Guid shiftId,
        Guid? actingUserId = null,
        string? actingUserRole = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShiftAssignmentResponseDto>> GetMyAssignmentsAsync(
        Guid userOrVolunteerId,
        CancellationToken cancellationToken = default);

    Task<ShiftAssignmentResponseDto?> GetByIdAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAssignmentAsync(
        Guid shiftId,
        Guid assignmentId,
        Guid? actingUserId = null,
        string? actingUserRole = null,
        CancellationToken cancellationToken = default);
}
