using EventCrew.Api.DTOs.Shifts;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

public class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly AppDbContext _db;

    public ShiftAssignmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ShiftAssignmentResponseDto> AssignVolunteerAsync(
        Guid shiftId,
        AssignVolunteerDto dto,
        Guid? actingUserId = null,
        string? actingUserRole = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Authorization: Volunteers cannot assign volunteers
        if (string.Equals(actingUserRole, "Volunteer", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Volunteers are not authorized to assign volunteers to shifts.");
        }

        // 2. Shift existence & Event loading
        var shift = await _db.Shifts
            .Include(s => s.Event)
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

        if (shift is null)
        {
            throw new KeyNotFoundException("Shift not found.");
        }

        // 3. Organizer authorization: Organizer must own the event unless Admin
        if (!string.Equals(actingUserRole, "Admin", StringComparison.OrdinalIgnoreCase) && actingUserId.HasValue)
        {
            if (shift.Event.OrganizerId != actingUserId.Value)
            {
                throw new UnauthorizedAccessException("You are not authorized to manage assignments for this event.");
            }
        }

        // 4. VolunteerProfile existence
        var volunteer = await _db.VolunteerProfiles
            .Include(vp => vp.User)
            .FirstOrDefaultAsync(vp => vp.Id == dto.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            throw new KeyNotFoundException("Volunteer profile not found.");
        }

        // 5. Eligibility: Must have an Accepted or Assigned Application for this event
        var application = await _db.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.EventId == shift.EventId && a.VolunteerId == dto.VolunteerId, cancellationToken);

        if (application is null || !(string.Equals(application.Status, "Accepted", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(application.Status, "Assigned", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Volunteer is not eligible: Volunteer must have an Accepted or Assigned application for this event.");
        }

        // 6. Capacity check: Count only active assignments
        var activeCount = shift.Assignments
            .Count(a => a.Status != "Declined" && a.Status != "Cancelled");

        if (activeCount >= shift.Capacity)
        {
            throw new InvalidOperationException("Shift has reached maximum capacity.");
        }

        // 7. Duplicate check: Volunteer cannot be assigned twice to the same shift
        var alreadyAssigned = shift.Assignments
            .Any(a => a.VolunteerId == dto.VolunteerId && a.Status != "Declined" && a.Status != "Cancelled");

        if (alreadyAssigned)
        {
            throw new InvalidOperationException("Volunteer is already assigned to this shift.");
        }

        // 8. Create assignment record
        var assignment = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shiftId,
            VolunteerId = dto.VolunteerId,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Confirmed" : dto.Status.Trim(),
            AssignedByUserId = actingUserId,
            AssignedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.ShiftAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        assignment.Shift = shift;
        assignment.Volunteer = volunteer;

        return MapToDto(assignment);
    }

    public async Task<IReadOnlyList<ShiftAssignmentResponseDto>> GetAssignmentsByShiftIdAsync(
        Guid shiftId,
        Guid? actingUserId = null,
        string? actingUserRole = null,
        CancellationToken cancellationToken = default)
    {
        var shift = await _db.Shifts
            .Include(s => s.Event)
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

        if (shift is null)
        {
            throw new KeyNotFoundException("Shift not found.");
        }

        if (!string.Equals(actingUserRole, "Admin", StringComparison.OrdinalIgnoreCase) && actingUserId.HasValue)
        {
            if (shift.Event.OrganizerId != actingUserId.Value)
            {
                throw new UnauthorizedAccessException("You are not authorized to view assignments for this event.");
            }
        }

        var assignments = await _db.ShiftAssignments
            .AsNoTracking()
            .Include(a => a.Shift)
                .ThenInclude(s => s.Event)
            .Include(a => a.Volunteer)
                .ThenInclude(vp => vp.User)
            .Where(a => a.ShiftId == shiftId)
            .OrderBy(a => a.AssignedAt)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ShiftAssignmentResponseDto>> GetMyAssignmentsAsync(
        Guid userOrVolunteerId,
        CancellationToken cancellationToken = default)
    {
        // Resolve volunteer profile ID (handles whether userOrVolunteerId is User.Id or VolunteerProfile.Id)
        var profile = await _db.VolunteerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(vp => vp.Id == userOrVolunteerId || vp.UserId == userOrVolunteerId, cancellationToken);

        if (profile is null)
        {
            return Array.Empty<ShiftAssignmentResponseDto>();
        }

        var assignments = await _db.ShiftAssignments
            .AsNoTracking()
            .Include(a => a.Shift)
                .ThenInclude(s => s.Event)
            .Include(a => a.Volunteer)
                .ThenInclude(vp => vp.User)
            .Where(a => a.VolunteerId == profile.Id && a.Status != "Cancelled" && a.Status != "Declined")
            .OrderBy(a => a.Shift.StartTime)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<ShiftAssignmentResponseDto?> GetByIdAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _db.ShiftAssignments
            .AsNoTracking()
            .Include(a => a.Shift)
                .ThenInclude(s => s.Event)
            .Include(a => a.Volunteer)
                .ThenInclude(vp => vp.User)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken);

        return assignment is null ? null : MapToDto(assignment);
    }

    public async Task<bool> RemoveAssignmentAsync(
        Guid shiftId,
        Guid assignmentId,
        Guid? actingUserId = null,
        string? actingUserRole = null,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _db.ShiftAssignments
            .Include(a => a.Shift)
                .ThenInclude(s => s.Event)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.ShiftId == shiftId, cancellationToken);

        if (assignment is null)
        {
            return false;
        }

        if (!string.Equals(actingUserRole, "Admin", StringComparison.OrdinalIgnoreCase) && actingUserId.HasValue)
        {
            if (assignment.Shift.Event.OrganizerId != actingUserId.Value)
            {
                throw new UnauthorizedAccessException("You are not authorized to manage assignments for this event.");
            }
        }

        _db.ShiftAssignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ShiftAssignmentResponseDto MapToDto(ShiftAssignment a)
    {
        return new ShiftAssignmentResponseDto
        {
            Id = a.Id,
            ShiftId = a.ShiftId,
            ShiftTitle = a.Shift?.Title,
            EventId = a.Shift?.EventId ?? Guid.Empty,
            EventTitle = a.Shift?.Event?.Title,
            StartTime = a.Shift?.StartTime ?? DateTimeOffset.MinValue,
            EndTime = a.Shift?.EndTime ?? DateTimeOffset.MinValue,
            VolunteerId = a.VolunteerId,
            VolunteerName = a.Volunteer?.User?.FullName,
            VolunteerEmail = a.Volunteer?.User?.Email,
            Status = a.Status,
            AssignedByUserId = a.AssignedByUserId,
            AssignedAt = a.AssignedAt,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }
}
