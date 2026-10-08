using EventCrew.Api.DTOs.Shifts;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

/// <summary>
/// Implements the shift swap workflow:
///   Volunteer requests swap → Target volunteer accepts/declines → Organizer approves/rejects → Assignments exchanged.
/// </summary>
public class ShiftSwapService : IShiftSwapService
{
    private static readonly HashSet<string> ValidStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Pending_Target", "Pending_Organizer", "Approved", "Rejected", "Cancelled"
        };

    private readonly AppDbContext _db;

    public ShiftSwapService(AppDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ShiftSwapResponseDto> CreateSwapRequestAsync(
        CreateSwapRequestDto dto,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve the requester's volunteer profile
        var requesterProfile = await _db.VolunteerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(vp => vp.UserId == requestingUserId || vp.Id == requestingUserId, cancellationToken);

        if (requesterProfile is null)
            throw new KeyNotFoundException("Requester volunteer profile not found.");

        // 2. Load the requester's assignment (must belong to them)
        var requesterAssignment = await _db.ShiftAssignments
            .Include(a => a.Shift)
            .FirstOrDefaultAsync(a => a.Id == dto.RequesterAssignmentId, cancellationToken);

        if (requesterAssignment is null)
            throw new KeyNotFoundException("Requester shift assignment not found.");

        if (requesterAssignment.VolunteerId != requesterProfile.Id)
            throw new UnauthorizedAccessException("You can only swap your own shift assignment.");

        if (requesterAssignment.Status == "Cancelled" || requesterAssignment.Status == "Declined")
            throw new InvalidOperationException("Cannot swap a cancelled or declined assignment.");

        // 3. Target volunteer must exist
        var targetVolunteer = await _db.VolunteerProfiles
            .Include(vp => vp.User)
            .FirstOrDefaultAsync(vp => vp.Id == dto.TargetVolunteerId, cancellationToken);

        if (targetVolunteer is null)
            throw new KeyNotFoundException("Target volunteer profile not found.");

        // 4. Target shift must exist
        var targetShift = await _db.Shifts
            .Include(s => s.Event)
            .FirstOrDefaultAsync(s => s.Id == dto.TargetShiftId, cancellationToken);

        if (targetShift is null)
            throw new KeyNotFoundException("Target shift not found.");

        // 5. Target volunteer must have an active assignment on the target shift
        var targetAssignment = await _db.ShiftAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.ShiftId == dto.TargetShiftId &&
                     a.VolunteerId == dto.TargetVolunteerId &&
                     a.Status != "Cancelled" && a.Status != "Declined",
                cancellationToken);

        if (targetAssignment is null)
            throw new InvalidOperationException(
                "Target volunteer does not have an active assignment on the specified target shift.");

        // 6. Prevent self-swap
        if (dto.TargetVolunteerId == requesterProfile.Id)
            throw new InvalidOperationException("You cannot create a swap request with yourself.");

        // 7. No duplicate pending swap for the same requester assignment
        var duplicatePending = await _db.ShiftSwapRequests
            .AsNoTracking()
            .AnyAsync(
                r => r.RequesterAssignmentId == dto.RequesterAssignmentId &&
                     (r.Status == "Pending_Target" || r.Status == "Pending_Organizer"),
                cancellationToken);

        if (duplicatePending)
            throw new InvalidOperationException(
                "A pending swap request already exists for this assignment.");

        // 8. Create the swap request
        var swap = new ShiftSwapRequest
        {
            Id = Guid.NewGuid(),
            RequesterAssignmentId = dto.RequesterAssignmentId,
            TargetVolunteerId = dto.TargetVolunteerId,
            TargetShiftId = dto.TargetShiftId,
            Reason = dto.Reason,
            Status = "Pending_Target",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.ShiftSwapRequests.Add(swap);
        await _db.SaveChangesAsync(cancellationToken);

        swap.RequesterAssignment = requesterAssignment;
        swap.TargetVolunteer = targetVolunteer;
        swap.TargetShift = targetShift;

        return await MapToDtoAsync(swap, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE STATUS
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ShiftSwapResponseDto> UpdateSwapStatusAsync(
        Guid swapRequestId,
        UpdateSwapStatusDto dto,
        Guid actingUserId,
        string actingUserRole,
        CancellationToken cancellationToken = default)
    {
        var newStatus = dto.Status.Trim();

        if (!ValidStatuses.Contains(newStatus))
            throw new ArgumentException($"Invalid status '{newStatus}'.");

        var swap = await _db.ShiftSwapRequests
            .Include(r => r.RequesterAssignment)
                .ThenInclude(a => a.Shift)
                    .ThenInclude(s => s.Event)
            .Include(r => r.TargetVolunteer)
                .ThenInclude(vp => vp.User)
            .Include(r => r.TargetShift)
            .FirstOrDefaultAsync(r => r.Id == swapRequestId, cancellationToken);

        if (swap is null)
            throw new KeyNotFoundException("Swap request not found.");

        var isAdmin = string.Equals(actingUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
        var isOrganizer = string.Equals(actingUserRole, "Organizer", StringComparison.OrdinalIgnoreCase);

        // Resolve the acting user's volunteer profile (if they're a volunteer)
        var actingProfile = await _db.VolunteerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(vp => vp.UserId == actingUserId || vp.Id == actingUserId, cancellationToken);

        var isTargetVolunteer = actingProfile is not null && actingProfile.Id == swap.TargetVolunteerId;
        var isRequester = actingProfile is not null &&
                          actingProfile.Id == swap.RequesterAssignment.VolunteerId;

        // ── Authorization & allowed transitions ─────────────────────────────
        switch (swap.Status)
        {
            case "Pending_Target":
                // Target volunteer: accept → Pending_Organizer, decline → Rejected
                // Requester: cancel → Cancelled
                if (newStatus == "Cancelled")
                {
                    if (!isRequester && !isAdmin)
                        throw new UnauthorizedAccessException("Only the requester or an admin can cancel this swap request.");
                }
                else if (newStatus == "Pending_Organizer")
                {
                    if (!isTargetVolunteer && !isAdmin)
                        throw new UnauthorizedAccessException("Only the target volunteer or an admin can accept this swap request.");
                }
                else if (newStatus == "Rejected")
                {
                    if (!isTargetVolunteer && !isAdmin)
                        throw new UnauthorizedAccessException("Only the target volunteer or an admin can reject this swap request at this stage.");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot transition from '{swap.Status}' to '{newStatus}'.");
                }
                break;

            case "Pending_Organizer":
                // Organizer / Admin: approve → Approved, reject → Rejected
                // Requester can still cancel
                if (newStatus == "Cancelled")
                {
                    if (!isRequester && !isAdmin)
                        throw new UnauthorizedAccessException("Only the requester or an admin can cancel this swap request.");
                }
                else if (newStatus == "Approved" || newStatus == "Rejected")
                {
                    if (!isOrganizer && !isAdmin)
                        throw new UnauthorizedAccessException("Only an organizer or admin can approve/reject this swap request.");

                    // Organizer must own the event (unless Admin)
                    if (!isAdmin)
                    {
                        var eventOrganizerId = swap.RequesterAssignment.Shift.Event.OrganizerId;
                        if (eventOrganizerId != actingUserId)
                            throw new UnauthorizedAccessException("You are not authorized to manage swaps for this event.");
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot transition from '{swap.Status}' to '{newStatus}'.");
                }
                break;

            default:
                // Approved / Rejected / Cancelled are terminal
                throw new InvalidOperationException(
                    $"Swap request is already in a terminal state: '{swap.Status}'.");
        }

        // ── Apply the status transition ──────────────────────────────────────
        swap.Status = newStatus;
        swap.UpdatedAt = DateTimeOffset.UtcNow;

        // ── If approved: swap the assignments ────────────────────────────────
        if (newStatus == "Approved")
        {
            await ExecuteSwapAsync(swap, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(swap, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // QUERIES
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ShiftSwapResponseDto>> GetMySwapRequestsAsync(
        Guid userOrVolunteerId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.VolunteerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(vp => vp.Id == userOrVolunteerId || vp.UserId == userOrVolunteerId, cancellationToken);

        if (profile is null)
            return Array.Empty<ShiftSwapResponseDto>();

        var swaps = await _db.ShiftSwapRequests
            .AsNoTracking()
            .Include(r => r.RequesterAssignment)
                .ThenInclude(a => a.Volunteer)
                    .ThenInclude(vp => vp.User)
            .Include(r => r.RequesterAssignment)
                .ThenInclude(a => a.Shift)
                    .ThenInclude(s => s.Event)
            .Include(r => r.TargetVolunteer)
                .ThenInclude(vp => vp.User)
            .Include(r => r.TargetShift)
            .Where(r =>
                r.RequesterAssignment.VolunteerId == profile.Id ||
                r.TargetVolunteerId == profile.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var tasks = swaps.Select(s => MapToDtoAsync(s, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    public async Task<IReadOnlyList<ShiftSwapResponseDto>> GetSwapRequestsByEventAsync(
        Guid eventId,
        Guid actingUserId,
        string actingUserRole,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = string.Equals(actingUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

        // Verify event exists and acting user is the organizer (or admin)
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (ev is null)
            throw new KeyNotFoundException("Event not found.");

        if (!isAdmin && ev.OrganizerId != actingUserId)
            throw new UnauthorizedAccessException("You are not authorized to view swap requests for this event.");

        var swaps = await _db.ShiftSwapRequests
            .AsNoTracking()
            .Include(r => r.RequesterAssignment)
                .ThenInclude(a => a.Volunteer)
                    .ThenInclude(vp => vp.User)
            .Include(r => r.RequesterAssignment)
                .ThenInclude(a => a.Shift)
                    .ThenInclude(s => s.Event)
            .Include(r => r.TargetVolunteer)
                .ThenInclude(vp => vp.User)
            .Include(r => r.TargetShift)
            .Where(r => r.RequesterAssignment.Shift.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var tasks = swaps.Select(s => MapToDtoAsync(s, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    public async Task<ShiftSwapResponseDto?> GetByIdAsync(
        Guid swapRequestId,
        CancellationToken cancellationToken = default)
    {
        var swap = await _db.ShiftSwapRequests
            .AsNoTracking()
            .Include(r => r.RequesterAssignment)
                .ThenInclude(a => a.Volunteer)
                    .ThenInclude(vp => vp.User)
            .Include(r => r.RequesterAssignment)
                .ThenInclude(a => a.Shift)
                    .ThenInclude(s => s.Event)
            .Include(r => r.TargetVolunteer)
                .ThenInclude(vp => vp.User)
            .Include(r => r.TargetShift)
            .FirstOrDefaultAsync(r => r.Id == swapRequestId, cancellationToken);

        return swap is null ? null : await MapToDtoAsync(swap, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the physical swap: exchanges shift IDs on both assignments.
    /// Requires the swap's navigation properties to already be loaded.
    /// </summary>
    private async Task ExecuteSwapAsync(ShiftSwapRequest swap, CancellationToken cancellationToken)
    {
        // Load both tracked assignments
        var requesterAssignment = await _db.ShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == swap.RequesterAssignmentId, cancellationToken);

        var targetAssignment = await _db.ShiftAssignments
            .FirstOrDefaultAsync(
                a => a.ShiftId == swap.TargetShiftId &&
                     a.VolunteerId == swap.TargetVolunteerId &&
                     a.Status != "Cancelled" && a.Status != "Declined",
                cancellationToken);

        if (requesterAssignment is null)
            throw new InvalidOperationException("Requester assignment no longer exists.");

        if (targetAssignment is null)
            throw new InvalidOperationException("Target assignment no longer exists.");

        // Swap shift IDs
        var tempShiftId = requesterAssignment.ShiftId;
        requesterAssignment.ShiftId = targetAssignment.ShiftId;
        targetAssignment.ShiftId = tempShiftId;

        requesterAssignment.UpdatedAt = DateTimeOffset.UtcNow;
        targetAssignment.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task<ShiftSwapResponseDto> MapToDtoAsync(ShiftSwapRequest r, CancellationToken ct)
    {
        // Requester shift is the shift from the requester's assignment
        var requesterShift = r.RequesterAssignment?.Shift;

        // If TargetShift nav is not loaded, try loading
        var targetShift = r.TargetShift;
        if (targetShift is null && r.TargetShiftId != Guid.Empty)
        {
            targetShift = await _db.Shifts
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == r.TargetShiftId, ct);
        }

        return new ShiftSwapResponseDto
        {
            Id = r.Id,

            RequesterAssignmentId = r.RequesterAssignmentId,
            RequesterVolunteerId = r.RequesterAssignment?.VolunteerId ?? Guid.Empty,
            RequesterVolunteerName = r.RequesterAssignment?.Volunteer?.User?.FullName,
            RequesterShiftId = requesterShift?.Id ?? Guid.Empty,
            RequesterShiftTitle = requesterShift?.Title,
            RequesterShiftStartTime = requesterShift?.StartTime ?? DateTimeOffset.MinValue,
            RequesterShiftEndTime = requesterShift?.EndTime ?? DateTimeOffset.MinValue,

            TargetVolunteerId = r.TargetVolunteerId,
            TargetVolunteerName = r.TargetVolunteer?.User?.FullName,
            TargetShiftId = r.TargetShiftId,
            TargetShiftTitle = targetShift?.Title,
            TargetShiftStartTime = targetShift?.StartTime ?? DateTimeOffset.MinValue,
            TargetShiftEndTime = targetShift?.EndTime ?? DateTimeOffset.MinValue,

            Reason = r.Reason,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
