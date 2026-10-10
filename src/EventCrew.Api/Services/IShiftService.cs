using EventCrew.Api.DTOs.Shifts;

namespace EventCrew.Api.Services;

public interface IShiftService
{
    Task<IReadOnlyList<ShiftResponseDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ShiftResponseDto>> GetByEventAsync(Guid eventId, CancellationToken ct = default);
    Task<ShiftResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ShiftResponseDto> CreateAsync(CreateShiftDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<ShiftAssignmentResponseDto?> AssignVolunteerAsync(
        Guid shiftId, AssignVolunteerDto dto, Guid assignedByUserId, CancellationToken ct = default);

    Task<bool> RemoveAssignmentAsync(Guid shiftId, Guid assignmentId, CancellationToken ct = default);

    /// <summary>Returns shifts assigned to the currently authenticated volunteer.</summary>
    Task<IReadOnlyList<ShiftResponseDto>> GetMyShiftsAsync(Guid volunteerId, CancellationToken ct = default);

    Task<ShiftAssignmentResponseDto?> ConfirmAssignmentAsync(
        Guid assignmentId, Guid volunteerId, CancellationToken ct = default);

    // ---- Swap requests ----
    Task<ShiftSwapResponseDto> CreateSwapRequestAsync(
        Guid requesterVolunteerId, CreateSwapRequestDto dto, CancellationToken ct = default);

    Task<ShiftSwapResponseDto?> ApproveSwapAsync(
        Guid swapId, Guid reviewedByUserId, CancellationToken ct = default);

    Task<ShiftSwapResponseDto?> RejectSwapAsync(
        Guid swapId, Guid reviewedByUserId, string reason, CancellationToken ct = default);

    Task<IReadOnlyList<ShiftSwapResponseDto>> GetSwapRequestsForVolunteerAsync(
        Guid volunteerId, CancellationToken ct = default);
}