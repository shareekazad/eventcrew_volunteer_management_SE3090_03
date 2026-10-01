using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventCrew.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventCrew.Api.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        var keyBytes = Encoding.UTF8.GetBytes(_options.Key);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must contain at least 32 UTF-8 bytes.");
        }

        if (string.IsNullOrWhiteSpace(_options.Issuer) || string.IsNullOrWhiteSpace(_options.Audience) || _options.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException("JWT issuer, audience, and positive access-token lifetime are required.");
        }

        _credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
    }

    public GeneratedAccessToken CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.FullName),
            new Claim("role", user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _credentials);

        return new GeneratedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), new DateTimeOffset(expires));
    }
}
