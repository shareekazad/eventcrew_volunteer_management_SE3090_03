using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventCrew.Api.DTOs.Auth;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EventCrew.Api.Services;

/// <summary>
/// Authentication service: register, login, and current-user lookup.
/// Hashes passwords with BCrypt and issues JWT tokens.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    // ============================================================
    // REGISTER
    // ============================================================
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            Role = "Volunteer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            IsActive = true,
        };

        var profile = new VolunteerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EmergencyContact = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? "Not provided" : dto.PhoneNumber.Trim(),
            Bio = null,
            MaxHoursPerWeek = 20,
            RatingScore = 5.00m,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _db.Users.Add(user);
        _db.VolunteerProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered new volunteer {UserId} ({Email})", user.Id, user.Email);

        return BuildAuthResponse(user);
    }

    // ============================================================
    // LOGIN
    // ============================================================
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login attempt for unknown email {Email}", normalizedEmail);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated user {UserId}", user.Id);
            return null;
        }

        bool passwordValid;
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        }
        catch
        {
            // Stored hash is not a valid BCrypt hash (e.g., legacy/seed data).
            // Reject safely instead of throwing.
            passwordValid = false;
        }

        if (!passwordValid)
        {
            _logger.LogWarning("Invalid password for user {UserId}", user.Id);
            return null;
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return BuildAuthResponse(user);
    }

    // ============================================================
    // CURRENT USER
    // ============================================================
    public async Task<AuthResponseDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : BuildAuthResponse(user);
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private AuthResponseDto BuildAuthResponse(User user)
    {
        var (token, expiresAt) = GenerateJwt(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Token = token,
            ExpiresAt = expiresAt,
        };
    }

    private (string token, DateTimeOffset expiresAt) GenerateJwt(User user)
    {
        var jwtKey = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
        if (string.IsNullOrWhiteSpace(jwtKey))
            jwtKey = "DevFallbackSecretKeyForLocalTestingOnly12345!";

        var jwtIssuer = _config["Jwt:Issuer"] ?? "EventCrew";
        var jwtAudience = _config["Jwt:Audience"] ?? "EventCrewUsers";

        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }
}