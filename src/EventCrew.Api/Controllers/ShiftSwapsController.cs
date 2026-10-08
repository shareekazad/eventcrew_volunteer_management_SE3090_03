using System.Security.Claims;
using EventCrew.Api.DTOs.Shifts;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

/// <summary>
/// Manages shift swap requests.
/// Volunteers request swaps; target volunteers accept/decline; organizers approve/reject.
/// </summary>
[ApiController]
[Route("api/shift-swaps")]
public class ShiftSwapsController : ControllerBase
{
    private readonly IShiftSwapService _swapService;

    public ShiftSwapsController(IShiftSwapService swapService)
    {
        _swapService = swapService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/shift-swaps
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Volunteer creates a shift swap request.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShiftSwapResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShiftSwapResponseDto>> CreateSwapRequest(
        [FromBody] CreateSwapRequestDto dto,
        CancellationToken cancellationToken)
    {
        var (userId, _) = ResolveUserIdentity();
        if (!userId.HasValue)
            return Unauthorized(new { error = "Authentication required." });

        try
        {
            var result = await _swapService.CreateSwapRequestAsync(dto, userId.Value, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("pending swap request already exists", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status409Conflict, new { error = ex.Message });

            return BadRequest(new { error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATCH /api/shift-swaps/{id}/status
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update the status of a swap request.
    /// Target volunteer: Pending_Organizer (accept) or Rejected (decline).
    /// Requester: Cancelled.
    /// Organizer/Admin: Approved or Rejected.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ShiftSwapResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShiftSwapResponseDto>> UpdateSwapStatus(
        Guid id,
        [FromBody] UpdateSwapStatusDto dto,
        CancellationToken cancellationToken)
    {
        var (userId, userRole) = ResolveUserIdentity();
        if (!userId.HasValue)
            return Unauthorized(new { error = "Authentication required." });

        try
        {
            var result = await _swapService.UpdateSwapStatusAsync(
                id, dto, userId.Value, userRole ?? string.Empty, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/shift-swaps/my
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns all swap requests for the authenticated volunteer (as requester or target).</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftSwapResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ShiftSwapResponseDto>>> GetMySwapRequests(
        CancellationToken cancellationToken)
    {
        var (userId, _) = ResolveUserIdentity();
        if (!userId.HasValue)
            return Unauthorized(new { error = "Authentication required." });

        var list = await _swapService.GetMySwapRequestsAsync(userId.Value, cancellationToken);
        return Ok(list);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/shift-swaps/event/{eventId}
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns all swap requests for an event (Organizer/Admin only).</summary>
    [HttpGet("event/{eventId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftSwapResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ShiftSwapResponseDto>>> GetSwapRequestsByEvent(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var (userId, userRole) = ResolveUserIdentity();
        if (!userId.HasValue)
            return Unauthorized(new { error = "Authentication required." });

        try
        {
            var list = await _swapService.GetSwapRequestsByEventAsync(
                eventId, userId.Value, userRole ?? string.Empty, cancellationToken);
            return Ok(list);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/shift-swaps/{id}
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns a single swap request by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShiftSwapResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShiftSwapResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _swapService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private (Guid? userId, string? userRole) ResolveUserIdentity()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");

        if (Guid.TryParse(sub, out var parsedSub))
            return (parsedSub, role);

        if (Request.Headers.TryGetValue("X-User-Id", out var headerUserId) &&
            Guid.TryParse(headerUserId, out var parsedHeaderId))
        {
            var headerRole = Request.Headers.TryGetValue("X-Role", out var r) ? r.ToString() : role;
            return (parsedHeaderId, headerRole);
        }

        if (Request.Headers.TryGetValue("X-Volunteer-Id", out var volHeaderId) &&
            Guid.TryParse(volHeaderId, out var parsedVolId))
        {
            return (parsedVolId, "Volunteer");
        }

        return (null, role);
    }
}
