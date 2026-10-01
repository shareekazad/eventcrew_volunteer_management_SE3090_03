using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.Dtos;

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record AuthenticatedUserResponse(Guid Id, string FullName, string Email, string Role);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthenticatedUserResponse User);
