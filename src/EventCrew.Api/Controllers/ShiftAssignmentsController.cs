using System.Security.Claims;
using EventCrew.Api.DTOs.Shifts;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/shifts")]
public class ShiftAssignmentsController : ControllerBase
{
    private readonly IShiftAssignmentService _assignmentService;

    public ShiftAssignmentsController(IShiftAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>Assign an eligible volunteer to a shift.</summary>
    [HttpPost("{shiftId:guid}/assignments")]
    [ProducesResponseType(typeof(ShiftAssignmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShiftAssignmentResponseDto>> AssignVolunteer(
        Guid shiftId,
        [FromBody] AssignVolunteerDto dto,
        CancellationToken cancellationToken)
    {
        var (userId, userRole) = ResolveUserIdentity();

        try
        {
            var created = await _assignmentService.AssignVolunteerAsync(shiftId, dto, userId, userRole, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created);
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
            // If conflict (capacity or duplicate assignment)
            if (ex.Message.Contains("capacity", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("already assigned", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status409Conflict, new { error = ex.Message });
            }

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>List all volunteers assigned to a specific shift.</summary>
    [HttpGet("{shiftId:guid}/assignments")]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftAssignmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ShiftAssignmentResponseDto>>> GetShiftAssignments(
        Guid shiftId,
        CancellationToken cancellationToken)
    {
        var (userId, userRole) = ResolveUserIdentity();

        try
        {
            var list = await _assignmentService.GetAssignmentsByShiftIdAsync(shiftId, userId, userRole, cancellationToken);
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

    /// <summary>List shifts assigned to the authenticated volunteer.</summary>
    [HttpGet("my-assignments")]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftAssignmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ShiftAssignmentResponseDto>>> GetMyAssignments(
        CancellationToken cancellationToken)
    {
        var (userId, _) = ResolveUserIdentity();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User identity is required to view assignments." });
        }

        var list = await _assignmentService.GetMyAssignmentsAsync(userId.Value, cancellationToken);
        return Ok(list);
    }

    /// <summary>Get a single shift assignment by ID.</summary>
    [HttpGet("assignments/{id:guid}")]
    [ProducesResponseType(typeof(ShiftAssignmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShiftAssignmentResponseDto>> GetAssignmentById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignmentService.GetByIdAsync(id, cancellationToken);
        return assignment is null ? NotFound() : Ok(assignment);
    }

    /// <summary>Remove/unassign a volunteer from a shift.</summary>
    [HttpDelete("{shiftId:guid}/assignments/{assignmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignment(
        Guid shiftId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var (userId, userRole) = ResolveUserIdentity();

        try
        {
            var removed = await _assignmentService.RemoveAssignmentAsync(shiftId, assignmentId, userId, userRole, cancellationToken);
            return removed ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    private (Guid? userId, string? userRole) ResolveUserIdentity()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");

        if (Guid.TryParse(sub, out var parsedSub))
        {
            return (parsedSub, role);
        }

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

        if (Request.Headers.TryGetValue("X-Reviewer-Id", out var reviewerId) &&
            Guid.TryParse(reviewerId, out var parsedReviewerId))
        {
            return (parsedReviewerId, role);
        }

        return (null, role);
    }
}
