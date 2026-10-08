using System.Security.Claims;
using EventCrew.Api.DTOs.Shifts;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;

    public ShiftsController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    /// <summary>List all shifts with optional filtering by eventId, date, or status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShiftResponseDto>>> GetAll(
        [FromQuery] Guid? eventId,
        [FromQuery] DateTimeOffset? date,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var shifts = await _shiftService.GetAllAsync(eventId, date, status, cancellationToken);
        return Ok(shifts);
    }

    /// <summary>Get a shift by its ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShiftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShiftResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var shift = await _shiftService.GetByIdAsync(id, cancellationToken);
        return shift is null ? NotFound() : Ok(shift);
    }

    /// <summary>Create a new shift.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShiftResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShiftResponseDto>> Create(
        [FromBody] CreateShiftDto dto,
        CancellationToken cancellationToken)
    {
        var (userId, userRole) = ResolveUserIdentity();

        try
        {
            var created = await _shiftService.CreateAsync(dto, userId, userRole, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update an existing shift.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ShiftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShiftResponseDto>> Update(
        Guid id,
        [FromBody] UpdateShiftDto dto,
        CancellationToken cancellationToken)
    {
        var (userId, userRole) = ResolveUserIdentity();

        try
        {
            var updated = await _shiftService.UpdateAsync(id, dto, userId, userRole, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete a shift.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (userId, userRole) = ResolveUserIdentity();

        try
        {
            var deleted = await _shiftService.DeleteAsync(id, userId, userRole, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private (Guid? userId, string? userRole) ResolveUserIdentity()
    {
        // 1. Try JWT Claims
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");

        if (Guid.TryParse(sub, out var parsedSub))
        {
            return (parsedSub, role);
        }

        // 2. Dev Header Fallback
        if (Request.Headers.TryGetValue("X-User-Id", out var headerUserId) &&
            Guid.TryParse(headerUserId, out var parsedHeaderId))
        {
            var headerRole = Request.Headers.TryGetValue("X-Role", out var r) ? r.ToString() : role;
            return (parsedHeaderId, headerRole);
        }

        if (Request.Headers.TryGetValue("X-Reviewer-Id", out var reviewerId) &&
            Guid.TryParse(reviewerId, out var parsedReviewerId))
        {
            return (parsedReviewerId, role);
        }

        return (null, role);
    }
}
