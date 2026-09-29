using EventCrew.Api.DTOs;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCrew.Api.Controllers;

/// <summary>
/// Manages volunteer applications to events.
/// </summary>
[ApiController]
[Route("api/applications")]
[Produces("application/json")]
public class ApplicationsController : ControllerBase
{
    private readonly IVolunteerService _volunteerService;

    public ApplicationsController(IVolunteerService volunteerService)
    {
        _volunteerService = volunteerService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/applications
    // Volunteer submits an application to an event.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Submit a new application to an event.</summary>
    /// <remarks>Returns 409 Conflict if the volunteer has already applied to this event.</remarks>
    /// <response code="201">Application submitted successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">The volunteer has already applied for this event.</response>
    [HttpPost]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(ApplicationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply([FromBody] ApplyEventDto dto)
    {
        var volunteerId = GetCurrentVolunteerId();

        try
        {
            var result = await _volunteerService.ApplyForEventAsync(volunteerId, dto);
            return CreatedAtAction(nameof(GetApplicantsByEvent),
                new { eventId = result.EventId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/applications/event/{eventId}
    // Organizer / Admin retrieves a paginated, optionally filtered list of applicants.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Get all applicants for a specific event, with optional status filtering.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="status">Optional status filter (Submitted | UnderReview | Shortlisted | Accepted | Rejected).</param>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Results per page. Default: 20.</param>
    /// <response code="200">Applicant list returned.</response>
    [HttpGet("event/{eventId:guid}")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(IEnumerable<ApplicationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicantsByEvent(
        [FromRoute] Guid eventId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var results = await _volunteerService.GetApplicantsByEventIdAsync(eventId, status, page, pageSize);
        return Ok(results);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT /api/applications/{id}/status
    // Organizer / Admin advances the application through the status state-machine.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Update the status of a specific application.</summary>
    /// <param name="id">The application ID.</param>
    /// <remarks>
    /// Allowed transitions:
    /// Submitted → UnderReview → Shortlisted → Accepted | Rejected
    /// </remarks>
    /// <response code="200">Status updated successfully.</response>
    /// <response code="400">Illegal status transition or validation error.</response>
    /// <response code="404">Application not found.</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(ApplicationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateApplicationStatusDto dto)
    {
        try
        {
            var result = await _volunteerService.UpdateApplicationStatusAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the current volunteer's profile ID from the JWT subject claim.
    /// In a full implementation, this would look up the VolunteerProfile by UserId.
    /// Here we parse it directly as the profile GUID from the token for simplicity.
    /// </summary>
    private Guid GetCurrentVolunteerId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException("User ID claim is missing from the token.");

        return Guid.Parse(sub);
    }
}
