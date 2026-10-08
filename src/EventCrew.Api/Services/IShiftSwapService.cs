using EventCrew.Api.DTOs.Shifts;

namespace EventCrew.Api.Services;

public interface IShiftSwapService
{
    /// <summary>
    /// Volunteer creates a swap request to exchange their shift with a target volunteer.
    /// </summary>
    Task<ShiftSwapResponseDto> CreateSwapRequestAsync(
        CreateSwapRequestDto dto,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Target volunteer responds: "Pending_Organizer" (accept) or "Rejected" (decline).
    /// Requester may also cancel their own request: "Cancelled".
    /// Organizer / Admin may set: "Approved" or "Rejected".
    /// </summary>
    Task<ShiftSwapResponseDto> UpdateSwapStatusAsync(
        Guid swapRequestId,
        UpdateSwapStatusDto dto,
        Guid actingUserId,
        string actingUserRole,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all swap requests relevant to a volunteer (as requester or target).</summary>
    Task<IReadOnlyList<ShiftSwapResponseDto>> GetMySwapRequestsAsync(
        Guid userOrVolunteerId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all swap requests for an event (organizer / admin view).</summary>
    Task<IReadOnlyList<ShiftSwapResponseDto>> GetSwapRequestsByEventAsync(
        Guid eventId,
        Guid actingUserId,
        string actingUserRole,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single swap request by ID.</summary>
    Task<ShiftSwapResponseDto?> GetByIdAsync(
        Guid swapRequestId,
        CancellationToken cancellationToken = default);
}
