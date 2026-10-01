using EventCrew.Domain.Entities;

namespace EventCrew.Api.Services;

public interface IJwtTokenService
{
    GeneratedAccessToken CreateAccessToken(User user);
}

public sealed record GeneratedAccessToken(string AccessToken, DateTimeOffset ExpiresAt);
