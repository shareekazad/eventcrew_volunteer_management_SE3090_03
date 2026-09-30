using EventCrew.Api.DTOs;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCrew.Api.Controllers;

/// <summary>
/// Manages volunteer profiles.
/// </summary>
[ApiController]
[Route("api/volunteers")]
[Produces("application/json")]
public class VolunteersController : ControllerBase
{
    private readonly IVolunteerService _volunteerService;

    public VolunteersController(IVolunteerService volunteerService)
    {
        _volunteerService = volunteerService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/volunteers/profile
    // Creates or updates the volunteer profile of the currently authenticated user.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Create or update the volunteer profile for the authenticated user.</summary>
    /// <response code="201">Profile successfully created or updated.</response>
    /// <response code="400">Validation failed — see errors for details.</response>
    /// <response code="401">Unauthenticated request.</response>
    /// <response code="403">User does not hold the Volunteer role.</response>
    [HttpPost("profile")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(VolunteerProfileResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpsertProfile([FromBody] CreateVolunteerProfileDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _volunteerService.UpsertProfileAsync(userId, dto);
        return CreatedAtAction(nameof(GetMyProfile), result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/volunteers/me
    // Returns the volunteer profile of the currently authenticated user.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Get the volunteer profile of the currently authenticated user.</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="404">No profile found for this user — they must create one first.</response>
    [HttpGet("me")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(VolunteerProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId  = GetCurrentUserId();
        var profile = await _volunteerService.GetProfileByUserIdAsync(userId);

        if (profile is null)
            return NotFound(new { message = "No volunteer profile found. Please create one via POST /api/volunteers/profile." });

        return Ok(profile);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/volunteers/{id}
    // Public profile lookup — any authenticated user.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Get a volunteer's public profile by their profile ID.</summary>
    /// <param name="id">The volunteer profile ID (not the user ID).</param>
    /// <response code="200">Profile returned.</response>
    /// <response code="404">Profile not found.</response>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(VolunteerProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileById([FromRoute] Guid id)
    {
        var profile = await _volunteerService.GetProfileByIdAsync(id);

        if (profile is null)
            return NotFound(new { message = $"Volunteer profile {id} not found." });

        return Ok(profile);
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException("User ID claim is missing from the token.");

        return Guid.Parse(sub);
    }
}
