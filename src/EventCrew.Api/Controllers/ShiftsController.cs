using System.Security.Claims;
using EventCrew.Api.DTOs.Shifts;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

/// <summary>
/// Shift scheduling and rostering endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;
    private readonly ILogger<ShiftsController> _logger;

    public ShiftsController(IShiftService shiftService, ILogger<ShiftsController> logger)
    {
        _shiftService = shiftService;
        _logger = logger;
    }

    // ============================================================
    // READ
    // ============================================================

    /// <summary>Get all shifts (organizer view).</summary>
    [HttpGet]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var shifts = await _shiftService.GetAllAsync(ct);
        return Ok(shifts);
    }

    /// <summary>Get all shifts for a specific event.</summary>
    [HttpGet("by-event/{eventId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(Guid eventId, CancellationToken ct)
    {
        var shifts = await _shiftService.GetByEventAsync(eventId, ct);
        return Ok(shifts);
    }

    /// <summary>Get a single shift by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ShiftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var shift = await _shiftService.GetByIdAsync(id, ct);
        return shift is null ? NotFound() : Ok(shift);
    }

    /// <summary>Get all shifts assigned to the currently authenticated volunteer.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyShifts(CancellationToken ct)
    {
        var volunteerId = GetCurrentVolunteerId();
        if (volunteerId is null)
            return Unauthorized(new { error = "Invalid token: missing user id claim." });

        var shifts = await _shiftService.GetMyShiftsAsync(volunteerId.Value, ct);
        return Ok(shifts);
    }

    // ============================================================
    // CREATE / DELETE
    // ============================================================

    /// <summary>Create a new shift.</summary>
    [HttpPost]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(ShiftResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateShiftDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _shiftService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete a shift.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _shiftService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ============================================================
    // ASSIGNMENTS
    // ============================================================

    /// <summary>Assign a volunteer to a shift.</summary>
    [HttpPost("{shiftId:guid}/assign")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(ShiftAssignmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        Guid shiftId, [FromBody] AssignVolunteerDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token." });

        try
        {
            var assignment = await _shiftService.AssignVolunteerAsync(shiftId, dto, userId.Value, ct);
            return assignment is null ? NotFound() : StatusCode(StatusCodes.Status201Created, assignment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Remove a volunteer assignment from a shift.</summary>
    [HttpDelete("{shiftId:guid}/assignments/{assignmentId:guid}")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignment(
        Guid shiftId, Guid assignmentId, CancellationToken ct)
    {
        var deleted = await _shiftService.RemoveAssignmentAsync(shiftId, assignmentId, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Volunteer confirms their own assignment.</summary>
    [HttpPost("assignments/{assignmentId:guid}/confirm")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(ShiftAssignmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAssignment(Guid assignmentId, CancellationToken ct)
    {
        var volunteerId = GetCurrentVolunteerId();
        if (volunteerId is null)
            return Unauthorized(new { error = "Invalid token." });

        try
        {
            var updated = await _shiftService.ConfirmAssignmentAsync(assignmentId, volunteerId.Value, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================
    // SHIFT SWAPS
    // ============================================================

    /// <summary>Create a shift swap request.</summary>
    [HttpPost("swaps")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(ShiftSwapResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSwap(
        [FromBody] CreateSwapRequestDto dto, CancellationToken ct)
    {
        var volunteerId = GetCurrentVolunteerId();
        if (volunteerId is null)
            return Unauthorized(new { error = "Invalid token." });

        try
        {
            var swap = await _shiftService.CreateSwapRequestAsync(volunteerId.Value, dto, ct);
            return StatusCode(StatusCodes.Status201Created, swap);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Organizer approves a swap request.</summary>
    [HttpPost("swaps/{swapId:guid}/approve")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(ShiftSwapResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveSwap(Guid swapId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token." });

        try
        {
            var updated = await _shiftService.ApproveSwapAsync(swapId, userId.Value, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Organizer rejects a swap request.</summary>
    [HttpPost("swaps/{swapId:guid}/reject")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(ShiftSwapResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectSwap(
        Guid swapId, [FromBody] RejectSwapDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token." });

        try
        {
            var updated = await _shiftService.RejectSwapAsync(swapId, userId.Value, dto.Reason, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get swap requests involving the currently authenticated volunteer.</summary>
    [HttpGet("swaps/mine")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftSwapResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySwaps(CancellationToken ct)
    {
        var volunteerId = GetCurrentVolunteerId();
        if (volunteerId is null)
            return Unauthorized(new { error = "Invalid token." });

        var swaps = await _shiftService.GetSwapRequestsForVolunteerAsync(volunteerId.Value, ct);
        return Ok(swaps);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private Guid? GetCurrentVolunteerId() => GetCurrentUserId();
}

/// <summary>Input for rejecting a shift swap request.</summary>
public class RejectSwapDto
{
    public string Reason { get; set; } = string.Empty;
}