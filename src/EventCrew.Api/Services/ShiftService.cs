using EventCrew.Api.DTOs.Shifts;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

/// <summary>
/// Business logic for Shift Scheduling & Rostering.
/// Includes shift CRUD, volunteer assignment, and shift-swap workflow.
/// </summary>
public class ShiftService : IShiftService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ShiftService> _logger;

    public ShiftService(AppDbContext db, ILogger<ShiftService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ============================================================
    // READ
    // ============================================================

    public async Task<IReadOnlyList<ShiftResponseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var shifts = await _db.Shifts
            .AsNoTracking()
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Volunteer)
                    .ThenInclude(v => v.User)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

        return shifts.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ShiftResponseDto>> GetByEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var shifts = await _db.Shifts
            .AsNoTracking()
            .Where(s => s.EventId == eventId)
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Volunteer)
                    .ThenInclude(v => v.User)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

        return shifts.Select(MapToDto).ToList();
    }

    public async Task<ShiftResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var shift = await _db.Shifts
            .AsNoTracking()
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Volunteer)
                    .ThenInclude(v => v.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return shift is null ? null : MapToDto(shift);
    }

    public async Task<IReadOnlyList<ShiftResponseDto>> GetMyShiftsAsync(Guid volunteerId, CancellationToken ct = default)
    {
        var shifts = await _db.Shifts
            .AsNoTracking()
            .Where(s => s.Assignments.Any(a => a.VolunteerId == volunteerId))
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Volunteer)
                    .ThenInclude(v => v.User)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

        return shifts.Select(MapToDto).ToList();
    }

    // ============================================================
    // CREATE
    // ============================================================

    public async Task<ShiftResponseDto> CreateAsync(CreateShiftDto dto, CancellationToken ct = default)
    {
        if (dto.StartTime >= dto.EndTime)
            throw new InvalidOperationException("Start time must be before end time.");

        // Verify event exists
        var eventExists = await _db.Events.AnyAsync(e => e.Id == dto.EventId, ct);
        if (!eventExists)
            throw new InvalidOperationException("Event does not exist.");

        // Verify role requirement exists and belongs to that event
        var roleExists = await _db.RoleRequirements
            .AnyAsync(r => r.Id == dto.RoleRequirementId && r.EventId == dto.EventId, ct);
        if (!roleExists)
            throw new InvalidOperationException("Role requirement does not exist for this event.");

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = dto.EventId,
            RoleRequirementId = dto.RoleRequirementId,
            Title = dto.Title.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Capacity = dto.Capacity,
            Status = "Scheduled",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Shift {ShiftId} created for event {EventId}", shift.Id, shift.EventId);

        return MapToDto(shift);
    }

    // ============================================================
    // DELETE
    // ============================================================

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shift is null) return false;

        _db.Shifts.Remove(shift);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ============================================================
    // ASSIGN VOLUNTEER
    // ============================================================

    public async Task<ShiftAssignmentResponseDto?> AssignVolunteerAsync(
        Guid shiftId, AssignVolunteerDto dto, Guid assignedByUserId, CancellationToken ct = default)
    {
        var shift = await _db.Shifts
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == shiftId, ct);

        if (shift is null) return null;

        // Business rule: capacity check
        if (shift.Assignments.Count >= shift.Capacity)
            throw new InvalidOperationException(
                $"Shift '{shift.Title}' is full ({shift.Capacity}/{shift.Capacity}).");

        // Business rule: no duplicate assignment
        var alreadyAssigned = shift.Assignments.Any(a => a.VolunteerId == dto.VolunteerId);
        if (alreadyAssigned)
            throw new InvalidOperationException("Volunteer is already assigned to this shift.");

        // Verify volunteer profile exists
        var volunteerExists = await _db.VolunteerProfiles.AnyAsync(v => v.Id == dto.VolunteerId, ct);
        if (!volunteerExists)
            throw new InvalidOperationException("Volunteer profile does not exist.");

        var assignment = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shiftId,
            VolunteerId = dto.VolunteerId,
            Status = "Proposed_By_AI",
            AssignedByUserId = assignedByUserId,
            AssignedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.ShiftAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        // Load volunteer info for the response
        var volunteer = await _db.VolunteerProfiles
            .AsNoTracking()
            .Include(v => v.User)
            .FirstAsync(v => v.Id == dto.VolunteerId, ct);

        return new ShiftAssignmentResponseDto
        {
            Id = assignment.Id,
            ShiftId = assignment.ShiftId,
            VolunteerId = assignment.VolunteerId,
            VolunteerName = volunteer.User?.FullName,
            Status = assignment.Status,
            AssignedAt = assignment.AssignedAt
        };
    }

    public async Task<bool> RemoveAssignmentAsync(Guid shiftId, Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await _db.ShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.ShiftId == shiftId, ct);

        if (assignment is null) return false;

        _db.ShiftAssignments.Remove(assignment);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ============================================================
    // CONFIRM ASSIGNMENT (volunteer accepts)
    // ============================================================

    public async Task<ShiftAssignmentResponseDto?> ConfirmAssignmentAsync(
        Guid assignmentId, Guid volunteerId, CancellationToken ct = default)
    {
        var assignment = await _db.ShiftAssignments
            .Include(a => a.Volunteer)
                .ThenInclude(v => v.User)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);

        if (assignment is null) return null;

        if (assignment.VolunteerId != volunteerId)
            throw new InvalidOperationException("You can only confirm your own assignments.");

        assignment.Status = "Confirmed";
        assignment.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new ShiftAssignmentResponseDto
        {
            Id = assignment.Id,
            ShiftId = assignment.ShiftId,
            VolunteerId = assignment.VolunteerId,
            VolunteerName = assignment.Volunteer?.User?.FullName,
            Status = assignment.Status,
            AssignedAt = assignment.AssignedAt
        };
    }

    // ============================================================
    // SHIFT SWAP REQUESTS
    // ============================================================

    public async Task<ShiftSwapResponseDto> CreateSwapRequestAsync(
        Guid requesterVolunteerId, CreateSwapRequestDto dto, CancellationToken ct = default)
    {
        // Verify the requester owns the assignment
        var requesterAssignment = await _db.ShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == dto.RequesterAssignmentId, ct);

        if (requesterAssignment is null)
            throw new InvalidOperationException("Requester assignment not found.");

        if (requesterAssignment.VolunteerId != requesterVolunteerId)
            throw new InvalidOperationException("You can only swap your own assignments.");

        // Verify target volunteer
        var targetVolunteer = await _db.VolunteerProfiles
            .AnyAsync(v => v.Id == dto.TargetVolunteerId, ct);

        if (!targetVolunteer)
            throw new InvalidOperationException("Target volunteer profile not found.");

        // Verify target shift
        var targetShift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == dto.TargetShiftId, ct);
        if (targetShift is null)
            throw new InvalidOperationException("Target shift not found.");

        var swap = new ShiftSwapRequest
        {
            Id = Guid.NewGuid(),
            RequesterAssignmentId = dto.RequesterAssignmentId,
            TargetVolunteerId = dto.TargetVolunteerId,
            TargetShiftId = dto.TargetShiftId,
            Reason = dto.Reason?.Trim(),
            Status = "Pending_Target",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.ShiftSwapRequests.Add(swap);
        await _db.SaveChangesAsync(ct);

        return await MapSwapToDtoAsync(swap, ct);
    }

    public async Task<ShiftSwapResponseDto?> ApproveSwapAsync(
        Guid swapId, Guid reviewedByUserId, CancellationToken ct = default)
    {
        var swap = await _db.ShiftSwapRequests.FirstOrDefaultAsync(s => s.Id == swapId, ct);
        if (swap is null) return null;

        if (swap.Status != "Pending_Target" && swap.Status != "Pending_Organizer")
            throw new InvalidOperationException(
                $"Cannot approve a swap in '{swap.Status}' status.");

        swap.Status = "Approved";
        swap.UpdatedAt = DateTimeOffset.UtcNow;

        // Perform the actual swap: reassign the requester's assignment to the target volunteer
        var assignment = await _db.ShiftAssignments
            .FirstOrDefaultAsync(a => a.Id == swap.RequesterAssignmentId, ct);

        if (assignment is not null)
        {
            assignment.VolunteerId = swap.TargetVolunteerId;
            assignment.Status = "Confirmed";
            assignment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Swap {SwapId} approved by user {UserId}", swapId, reviewedByUserId);

        return await MapSwapToDtoAsync(swap, ct);
    }

    public async Task<ShiftSwapResponseDto?> RejectSwapAsync(
        Guid swapId, Guid reviewedByUserId, string reason, CancellationToken ct = default)
    {
        var swap = await _db.ShiftSwapRequests.FirstOrDefaultAsync(s => s.Id == swapId, ct);
        if (swap is null) return null;

        if (swap.Status == "Approved" || swap.Status == "Rejected" || swap.Status == "Cancelled")
            throw new InvalidOperationException(
                $"Cannot reject a swap in '{swap.Status}' status.");

        swap.Status = "Rejected";
        swap.Reason = string.IsNullOrWhiteSpace(reason)
            ? swap.Reason
            : $"{swap.Reason} | Organizer: {reason}".Trim(' ', '|');
        swap.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Swap {SwapId} rejected by user {UserId}: {Reason}", swapId, reviewedByUserId, reason);

        return await MapSwapToDtoAsync(swap, ct);
    }

    public async Task<IReadOnlyList<ShiftSwapResponseDto>> GetSwapRequestsForVolunteerAsync(
        Guid volunteerId, CancellationToken ct = default)
    {
        var swaps = await _db.ShiftSwapRequests
            .AsNoTracking()
            .Where(s =>
                _db.ShiftAssignments.Any(a =>
                    a.Id == s.RequesterAssignmentId && a.VolunteerId == volunteerId)
                || s.TargetVolunteerId == volunteerId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        var results = new List<ShiftSwapResponseDto>();
        foreach (var swap in swaps)
        {
            results.Add(await MapSwapToDtoAsync(swap, ct));
        }
        return results;
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    private static ShiftResponseDto MapToDto(Shift s) => new()
    {
        Id = s.Id,
        EventId = s.EventId,
        RoleRequirementId = s.RoleRequirementId,
        Title = s.Title,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Capacity = s.Capacity,
        Status = s.Status,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        AssignedCount = s.Assignments?.Count ?? 0,
        Assignments = s.Assignments?.Select(a => new ShiftAssignmentResponseDto
        {
            Id = a.Id,
            ShiftId = a.ShiftId,
            VolunteerId = a.VolunteerId,
            VolunteerName = a.Volunteer?.User?.FullName,
            Status = a.Status,
            AssignedAt = a.AssignedAt
        }).ToList() ?? new()
    };

    private async Task<ShiftSwapResponseDto> MapSwapToDtoAsync(ShiftSwapRequest swap, CancellationToken ct)
    {
        var targetVolunteer = await _db.VolunteerProfiles
            .AsNoTracking()
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == swap.TargetVolunteerId, ct);

        var targetShift = await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == swap.TargetShiftId, ct);

        return new ShiftSwapResponseDto
        {
            Id = swap.Id,
            RequesterAssignmentId = swap.RequesterAssignmentId,
            TargetVolunteerId = swap.TargetVolunteerId,
            TargetVolunteerName = targetVolunteer?.User?.FullName,
            TargetShiftId = swap.TargetShiftId,
            TargetShiftTitle = targetShift?.Title,
            Reason = swap.Reason,
            Status = swap.Status,
            CreatedAt = swap.CreatedAt,
            UpdatedAt = swap.UpdatedAt
        };
    }
}