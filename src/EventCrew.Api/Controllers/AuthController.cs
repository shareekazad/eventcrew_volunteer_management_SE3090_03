using EventCrew.Api.DTOs.Auth;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

/// <summary>
/// Provides authentication endpoints for user registration and login.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user account with role-based access.
    /// Automatically provisions a volunteer profile if role is 'Volunteer'.
    /// </summary>
    /// <response code="200">Registration successful. JWT token returned.</response>
    /// <response code="400">Validation error or invalid input.</response>
    /// <response code="409">Email address is already in use.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(dto, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user by email and password, issuing a JWT bearer token.
    /// </summary>
    /// <response code="200">Authentication successful. JWT token returned.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Invalid credentials or deactivated account.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(dto, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
