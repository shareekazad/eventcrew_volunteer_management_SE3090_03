using System.Security.Claims;
using EventCrew.Api.DTOs.Auth;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

/// <summary>
/// Authentication endpoints — register, login, and current-user lookup.
/// Called by the Flutter mobile app (volunteers) and the React web app (organizers).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // ============================================================
    // REGISTER
    // ============================================================
    /// <summary>
    /// Register a new volunteer. Creates the User + VolunteerProfile rows
    /// and returns a JWT so the client is immediately logged in.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for {Email}", dto.Email);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================
    // LOGIN
    // ============================================================
    /// <summary>
    /// Validate credentials and return a JWT token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);

        if (result is null)
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(result);
    }

    // ============================================================
    // ME — current authenticated user
    // ============================================================
    /// <summary>
    /// Returns basic info about the currently authenticated user.
    /// Requires a valid JWT in the Authorization header.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid token claims." });

        var result = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return result is null ? Unauthorized(new { error = "User not found." }) : Ok(result);
    }
}