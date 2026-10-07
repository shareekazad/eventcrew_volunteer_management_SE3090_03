using EventCrew.Api.DTOs.Auth;

namespace EventCrew.Api.Services;

/// <summary>
/// Contract for authentication: registration, login, and current-user lookup.
/// Issues JWT tokens for volunteers (Flutter app) and organizers (React app).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new volunteer. Creates a User row + VolunteerProfile row.
    /// Returns a JWT so the client is immediately logged in.
    /// Throws InvalidOperationException if the email is already registered.
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates credentials and returns a fresh JWT.
    /// Returns null if the email/password combination is invalid.
    /// </summary>
    Task<AuthResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns basic info for the currently authenticated user.
    /// Returns null if the user no longer exists.
    /// </summary>
    Task<AuthResponseDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}