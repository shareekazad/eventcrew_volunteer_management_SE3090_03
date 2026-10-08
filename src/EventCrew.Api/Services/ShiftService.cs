using EventCrew.Api.DTOs.Shifts;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

public class ShiftService : IShiftService
{
    private readonly AppDbContext _db;

    public ShiftService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ShiftResponseDto>> GetAllAsync(
        Guid? eventId = null,
        DateTimeOffset? date = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Shifts
            .AsNoTracking()
            .Include(s => s.Event)
            .Include(s => s.RoleRequirement)
            .Include(s => s.Assignments)
            .AsQueryable();

        if (eventId.HasValue)
        {
            query = query.Where(s => s.EventId == eventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status.ToLower() == status.Trim().ToLower());
        }

        if (date.HasValue)
        {
            var targetDate = date.Value.Date;
            var nextDate = targetDate.AddDays(1);
            query = query.Where(s => s.StartTime >= targetDate && s.StartTime < nextDate);
        }

        var shifts = await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return shifts.Select(MapToDto).ToList();
    }

    public async Task<ShiftResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await _db.Shifts
            .AsNoTracking()
            .Include(s => s.Event)
            .Include(s => s.RoleRequirement)
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return shift is null ? null : MapToDto(shift);
    }

    public async Task<ShiftResponseDto> CreateAsync(
        CreateShiftDto dto,
        Guid? userId = null,
        string? userRole = null,
        CancellationToken cancellationToken = default)
    {
        if (dto.StartTime >= dto.EndTime)
            throw new InvalidOperationException("End time must be after start time.");

        if (dto.Capacity <= 0)
            throw new InvalidOperationException("Capacity must be greater than zero.");

        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.Id == dto.EventId, cancellationToken);

        if (ev is null)
            throw new InvalidOperationException("Event does not exist.");

        ValidateOrganizerAuthorization(ev, userId, userRole);

        var roleExists = await _db.RoleRequirements
            .AnyAsync(r => r.Id == dto.RoleRequirementId && r.EventId == dto.EventId, cancellationToken);

        if (!roleExists)
            throw new InvalidOperationException("Role requirement does not belong to the specified event.");

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = dto.EventId,
            RoleRequirementId = dto.RoleRequirementId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Capacity = dto.Capacity,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Scheduled" : dto.Status.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync(cancellationToken);

        // Load navigations for mapping
        shift.Event = ev;
        shift.RoleRequirement = await _db.RoleRequirements
            .FirstAsync(r => r.Id == dto.RoleRequirementId, cancellationToken);

        return MapToDto(shift);
    }

    public async Task<ShiftResponseDto?> UpdateAsync(
        Guid id,
        UpdateShiftDto dto,
        Guid? userId = null,
        string? userRole = null,
        CancellationToken cancellationToken = default)
    {
        var shift = await _db.Shifts
            .Include(s => s.Event)
            .Include(s => s.RoleRequirement)
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shift is null)
            return null;

        ValidateOrganizerAuthorization(shift.Event, userId, userRole);

        if (dto.StartTime >= dto.EndTime)
            throw new InvalidOperationException("End time must be after start time.");

        if (dto.Capacity <= 0)
            throw new InvalidOperationException("Capacity must be greater than zero.");

        var activeAssignmentCount = shift.Assignments
            .Count(a => a.Status != "Declined" && a.Status != "Cancelled");

        if (dto.Capacity < activeAssignmentCount)
            throw new InvalidOperationException($"Capacity cannot be reduced below the current number of assignments ({activeAssignmentCount}).");

        if (dto.RoleRequirementId.HasValue && dto.RoleRequirementId.Value != shift.RoleRequirementId)
        {
            var roleExists = await _db.RoleRequirements
                .AnyAsync(r => r.Id == dto.RoleRequirementId.Value && r.EventId == shift.EventId, cancellationToken);

            if (!roleExists)
                throw new InvalidOperationException("Role requirement does not belong to the specified event.");

            shift.RoleRequirementId = dto.RoleRequirementId.Value;
            shift.RoleRequirement = await _db.RoleRequirements
                .FirstAsync(r => r.Id == dto.RoleRequirementId.Value, cancellationToken);
        }

        shift.Title = dto.Title.Trim();
        shift.Description = dto.Description?.Trim();
        shift.StartTime = dto.StartTime;
        shift.EndTime = dto.EndTime;
        shift.Capacity = dto.Capacity;

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            shift.Status = dto.Status.Trim();
        }

        shift.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(shift);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid? userId = null,
        string? userRole = null,
        CancellationToken cancellationToken = default)
    {
        var shift = await _db.Shifts
            .Include(s => s.Event)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shift is null)
            return false;

        ValidateOrganizerAuthorization(shift.Event, userId, userRole);

        _db.Shifts.Remove(shift);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateOrganizerAuthorization(Event ev, Guid? userId, string? userRole)
    {
        if (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            return;

        if (userId.HasValue && ev.OrganizerId != userId.Value)
            throw new UnauthorizedAccessException("You are not authorized to manage shifts for this event.");
    }

    private static ShiftResponseDto MapToDto(Shift shift)
    {
        var assignedCount = shift.Assignments?
            .Count(a => a.Status != "Declined" && a.Status != "Cancelled") ?? 0;

        return new ShiftResponseDto
        {
            Id = shift.Id,
            EventId = shift.EventId,
            EventTitle = shift.Event?.Title,
            RoleRequirementId = shift.RoleRequirementId,
            RoleName = shift.RoleRequirement?.RoleName,
            Title = shift.Title,
            Description = shift.Description,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Capacity = shift.Capacity,
            AssignedCount = assignedCount,
            RemainingCapacity = Math.Max(0, shift.Capacity - assignedCount),
            Status = shift.Status,
            CreatedAt = shift.CreatedAt,
            UpdatedAt = shift.UpdatedAt
        };
    }
}
