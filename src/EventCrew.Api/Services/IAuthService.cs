using EventCrew.Api.DTOs.Auth;

namespace EventCrew.Api.Services;

/// <summary>
/// Service contract for authentication, user registration, and JWT token issuing.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user. If role is "Volunteer", automatically creates an empty VolunteerProfile.
    /// Returns 409 Conflict (throws InvalidOperationException) if the email is already in use.
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user by email and password, returning an AuthResponseDto with a JWT token.
    /// Throws UnauthorizedAccessException if credentials are invalid.
    /// </summary>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
}
