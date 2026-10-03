using EventCrew.Api.DTOs.Events;
using EventCrew.Api.Services;
using EventCrew.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Authorize(Roles = AuthorizationRoles.All)]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly AppDbContext _dbContext;

    public EventsController(IEventService eventService, AppDbContext dbContext)
    {
        _eventService = eventService;
        _dbContext = dbContext;
    }

    // ---- Event CRUD ----

    /// <summary>Get all events (with nested role requirements).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var events = await _eventService.GetAllAsync(cancellationToken);
        if (!ResourceOwnership.IsOrganizer(User)) return Ok(events);

        var organizerId = ResourceOwnership.GetUserId(User);
        if (organizerId is null) return Ok(Array.Empty<EventResponseDto>());
        var ownedEventIds = await _dbContext.Events.AsNoTracking()
            .Where(eventEntity => eventEntity.OrganizerId == organizerId.Value)
            .Select(eventEntity => eventEntity.Id)
            .ToListAsync(cancellationToken);
        return Ok(events.Where(eventEntity => ownedEventIds.Contains(eventEntity.Id)).ToList());
    }

    /// <summary>Get an event by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (ResourceOwnership.IsOrganizer(User) && !await OwnsEventAsync(id, cancellationToken)) return NotFound();
        var ev = await _eventService.GetByIdAsync(id, cancellationToken);
        return ev is null ? NotFound() : Ok(ev);
    }

    /// <summary>Create a new event (optionally with nested role requirements).</summary>
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    [HttpPost]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponseDto>> Create(
        [FromBody] CreateEventDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (ResourceOwnership.IsOrganizer(User))
            {
                var organizerId = ResourceOwnership.GetUserId(User);
                if (organizerId is null) return Forbid();
                dto.OrganizerId = organizerId.Value;
            }
            var created = await _eventService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update an existing event (top-level fields only).</summary>
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> Update(
        Guid id,
        [FromBody] UpdateEventDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanManageEventAsync(id, cancellationToken)) return NotFound();
            var updated = await _eventService.UpdateAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete an event.</summary>
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanManageEventAsync(id, cancellationToken)) return NotFound();
        var deleted = await _eventService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // ---- Status transition ----

    /// <summary>Transition an event to a new status (validates allowed transitions).</summary>
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateEventStatusDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanManageEventAsync(id, cancellationToken)) return NotFound();
            var updated = await _eventService.UpdateStatusAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ---- Nested role requirements ----

    /// <summary>Add a role requirement to an event.</summary>
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    [HttpPost("{eventId:guid}/roles")]
    [ProducesResponseType(typeof(RoleRequirementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleRequirementDto>> AddRole(
        Guid eventId,
        [FromBody] RoleRequirementInputDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanManageEventAsync(eventId, cancellationToken)) return NotFound();
            var created = await _eventService.AddRoleAsync(eventId, dto, cancellationToken);
            return created is null ? NotFound() : StatusCode(StatusCodes.Status201Created, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Remove a role requirement from an event.</summary>
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    [HttpDelete("{eventId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(
        Guid eventId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await CanManageEventAsync(eventId, cancellationToken)) return NotFound();
            var deleted = await _eventService.RemoveRoleAsync(eventId, roleId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<bool> CanManageEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (ResourceOwnership.IsAdmin(User)) return await _dbContext.Events.AnyAsync(item => item.Id == eventId, cancellationToken);
        var organizerId = ResourceOwnership.GetUserId(User);
        if (!ResourceOwnership.IsOrganizer(User) || organizerId is null) return false;
        var eventOwnerId = await _dbContext.Events.AsNoTracking()
            .Where(item => item.Id == eventId)
            .Select(item => (Guid?)item.OrganizerId)
            .SingleOrDefaultAsync(cancellationToken);
        return eventOwnerId.HasValue && ResourceOwnership.CanManageEvent(User, eventOwnerId.Value);
    }

    private async Task<bool> OwnsEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var organizerId = ResourceOwnership.GetUserId(User);
        if (organizerId is null) return false;
        return await _dbContext.Events.AsNoTracking()
            .AnyAsync(item => item.Id == eventId && item.OrganizerId == organizerId.Value, cancellationToken);
    }
}
