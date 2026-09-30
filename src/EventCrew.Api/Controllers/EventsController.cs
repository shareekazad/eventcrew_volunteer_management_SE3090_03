using EventCrew.Api.DTOs.Events;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    // ---- Event CRUD ----

    /// <summary>Get all events (with nested role requirements).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventResponseDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _eventService.GetAllAsync(cancellationToken));

    /// <summary>Get an event by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ev = await _eventService.GetByIdAsync(id, cancellationToken);
        return ev is null ? NotFound() : Ok(ev);
    }

    /// <summary>Create a new event (optionally with nested role requirements).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponseDto>> Create(
        [FromBody] CreateEventDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _eventService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update an existing event (top-level fields only).</summary>
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
            var updated = await _eventService.UpdateAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete an event.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _eventService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // ---- Status transition ----

    /// <summary>Transition an event to a new status (validates allowed transitions).</summary>
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
            var created = await _eventService.AddRoleAsync(eventId, dto, cancellationToken);
            return created is null ? NotFound() : StatusCode(StatusCodes.Status201Created, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Remove a role requirement from an event.</summary>
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
            var deleted = await _eventService.RemoveRoleAsync(eventId, roleId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}