using EventCrew.Api.DTOs.Events;
using EventCrew.Domain.Entities;
using EventCrew.Domain.Enums;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

/// <summary>
/// Business logic for Event operations, including nested role requirements
/// and controlled status transitions.
/// </summary>
public class EventService : IEventService
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Legal status transitions. Key = current status, Value = allowed next statuses.
    /// </summary>
    private static readonly Dictionary<EventStatus, EventStatus[]> AllowedTransitions = new()
    {
        [EventStatus.Draft] = new[] { EventStatus.Published, EventStatus.Cancelled },
        [EventStatus.Published] = new[] { EventStatus.StaffingInProgress, EventStatus.Cancelled },
        [EventStatus.StaffingInProgress] = new[] { EventStatus.FullyStaffed, EventStatus.Published, EventStatus.Cancelled },
        [EventStatus.FullyStaffed] = new[] { EventStatus.Completed, EventStatus.StaffingInProgress, EventStatus.Cancelled },
        [EventStatus.Completed] = Array.Empty<EventStatus>(),   // terminal
        [EventStatus.Cancelled] = Array.Empty<EventStatus>()    // terminal
    };

    public EventService(AppDbContext db)
    {
        _db = db;
    }

    // ============================================================
    // READ
    // ============================================================

    public async Task<IReadOnlyList<EventResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var events = await _db.Events
            .AsNoTracking()
            .Include(e => e.RoleRequirements)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);

        return events.Select(MapToDto).ToList();
    }

    public async Task<EventResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Include(e => e.RoleRequirements)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return ev is null ? null : MapToDto(ev);
    }

    // ============================================================
    // CREATE
    // ============================================================

    public async Task<EventResponseDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        // Business rule: dates must be ordered
        if (dto.StartDate >= dto.EndDate)
            throw new InvalidOperationException("Start date must be before end date.");

        // Business rule: organizer must exist
        var organizerExists = await _db.Users
            .AnyAsync(u => u.Id == dto.OrganizerId, cancellationToken);
        if (!organizerExists)
            throw new InvalidOperationException("Organizer does not exist.");

        // Business rule: venue must exist if provided
        if (dto.VenueId.HasValue)
        {
            var venueExists = await _db.Venues
                .AnyAsync(v => v.Id == dto.VenueId.Value, cancellationToken);
            if (!venueExists)
                throw new InvalidOperationException("Venue does not exist.");
        }

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = dto.OrganizerId,
            VenueId = dto.VenueId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Category = dto.Category.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = EventStatus.Draft,   // server-controlled — always Draft on create
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Nested role requirements (validated in Part 2's helper)
        foreach (var roleInput in dto.RoleRequirements)
        {
            ev.RoleRequirements.Add(BuildRoleRequirement(roleInput));
        }

        _db.Events.Add(ev);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(ev);
    }

    // ============================================================
    // UPDATE
    // ============================================================

    public async Task<EventResponseDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .Include(e => e.RoleRequirements)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (ev is null)
            return null;

        if (dto.StartDate >= dto.EndDate)
            throw new InvalidOperationException("Start date must be before end date.");

        if (dto.VenueId.HasValue)
        {
            var venueExists = await _db.Venues
                .AnyAsync(v => v.Id == dto.VenueId.Value, cancellationToken);
            if (!venueExists)
                throw new InvalidOperationException("Venue does not exist.");
        }

        ev.VenueId = dto.VenueId;
        ev.Title = dto.Title.Trim();
        ev.Description = dto.Description?.Trim();
        ev.Category = dto.Category.Trim();
        ev.StartDate = dto.StartDate;
        ev.EndDate = dto.EndDate;
        ev.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return MapToDto(ev);
    }

    // ============================================================
    // DELETE
    // ============================================================

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (ev is null)
            return false;

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ============================================================
    // STATUS TRANSITION
    // ============================================================

    public async Task<EventResponseDto?> UpdateStatusAsync(Guid id, UpdateEventStatusDto dto, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .Include(e => e.RoleRequirements)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (ev is null)
            return null;

        if (!Enum.TryParse<EventStatus>(dto.NewStatus, ignoreCase: false, out var newStatus))
            throw new InvalidOperationException($"Unknown status '{dto.NewStatus}'.");

        var allowed = AllowedTransitions[ev.Status];
        if (!allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Cannot transition from '{ev.Status}' to '{newStatus}'.");

        ev.Status = newStatus;
        ev.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return MapToDto(ev);
    }

    // ============================================================
    // PLACEHOLDERS — will be filled in Part 2
    // ============================================================

        // ============================================================
    // NESTED ROLE REQUIREMENTS
    // ============================================================

    public async Task<RoleRequirementDto?> AddRoleAsync(Guid eventId, RoleRequirementInputDto dto, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .Include(e => e.RoleRequirements)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (ev is null)
            return null;

        // Business rule: cannot modify roles on a completed/cancelled event
        if (ev.Status is EventStatus.Completed or EventStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot add role requirements to an event in '{ev.Status}' status.");

        var role = BuildRoleRequirement(dto);
        ev.RoleRequirements.Add(role);
        ev.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return MapRoleToDto(role);
    }

    public async Task<bool> RemoveRoleAsync(Guid eventId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events
            .Include(e => e.RoleRequirements)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (ev is null)
            return false;

        if (ev.Status is EventStatus.Completed or EventStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot remove role requirements from an event in '{ev.Status}' status.");

        var role = ev.RoleRequirements.FirstOrDefault(r => r.Id == roleId);
        if (role is null)
            return false;

        ev.RoleRequirements.Remove(role);
        ev.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    private static RoleRequirement BuildRoleRequirement(RoleRequirementInputDto input)
    {
        if (!Enum.TryParse<ExperienceLevel>(input.MinExperienceLevel, ignoreCase: false, out var level))
            throw new InvalidOperationException($"Unknown experience level '{input.MinExperienceLevel}'.");

        return new RoleRequirement
        {
            Id = Guid.NewGuid(),
            RoleName = input.RoleName.Trim(),
            Description = input.Description?.Trim(),
            RequiredHeadcount = input.RequiredHeadcount,
            MinExperienceLevel = level,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static EventResponseDto MapToDto(Event ev) => new()
    {
        Id = ev.Id,
        OrganizerId = ev.OrganizerId,
        VenueId = ev.VenueId,
        Title = ev.Title,
        Description = ev.Description,
        Category = ev.Category,
        StartDate = ev.StartDate,
        EndDate = ev.EndDate,
        Status = ev.Status.ToString(),
        CreatedAt = ev.CreatedAt,
        UpdatedAt = ev.UpdatedAt,
        RoleRequirements = ev.RoleRequirements
            .OrderBy(r => r.RoleName)
            .Select(MapRoleToDto)
            .ToList()
    };

    private static RoleRequirementDto MapRoleToDto(RoleRequirement r) => new()
    {
        Id = r.Id,
        RoleName = r.RoleName,
        Description = r.Description,
        RequiredHeadcount = r.RequiredHeadcount,
        MinExperienceLevel = r.MinExperienceLevel.ToString(),
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}