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
/// Handles authentication, registration, BCrypt password hashing, and JWT token issuance.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        // 1. Check if email is already taken (returns 409 Conflict via InvalidOperationException)
        var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"User with email '{dto.Email.Trim()}' already exists.");
        }

        // Validate and normalize role
        var role = dto.Role?.Trim();
        if (string.Equals(role, "Organizer", StringComparison.OrdinalIgnoreCase))
        {
            role = "Organizer";
        }
        else if (string.Equals(role, "Volunteer", StringComparison.OrdinalIgnoreCase))
        {
            role = "Volunteer";
        }
        else
        {
            throw new ArgumentException("Role must be either 'Volunteer' or 'Organizer'.");
        }

        // 2. Hash password with BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // 3. Create User entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName.Trim(),
            Email = email,
            Role = role,
            PasswordHash = passwordHash,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            IsActive = true
        };
        _db.Users.Add(user);

        // 4. If role is "Volunteer", automatically initialize an empty VolunteerProfile
        if (string.Equals(user.Role, "Volunteer", StringComparison.OrdinalIgnoreCase))
        {
            var emergencyContact = !string.IsNullOrWhiteSpace(dto.PhoneNumber)
                ? (dto.PhoneNumber.Trim().Length > 20 ? dto.PhoneNumber.Trim()[..20] : dto.PhoneNumber.Trim())
                : "N/A";

            var profile = new VolunteerProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EmergencyContact = emergencyContact,
                Bio = string.Empty,
                MaxHoursPerWeek = 20,
                RatingScore = 5.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.VolunteerProfiles.Add(profile);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // 5. Generate JWT token and return user info
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Role = user.Role,
            FullName = user.FullName,
            Email = user.Email,
            UserId = user.Id
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        // 1. Find user by email
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // 2. Verify password with BCrypt (with legacy hash migration)
        bool passwordMatches;
        try
        {
            passwordMatches = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Legacy seed data: PasswordHash is a plaintext placeholder (e.g. "hashed_pw_123" or "hashed_default_password_dev_123"), not a BCrypt hash.
            // Accept the known dev placeholder and inline-migrate to a real BCrypt hash.
            if ((user.PasswordHash == "hashed_pw_123" || user.PasswordHash == "hashed_default_password_dev_123") && dto.Password == "password123")
            {
                passwordMatches = true;
                // Upgrade hash in-place so all future logins use real BCrypt
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                passwordMatches = false;
            }
        }

        if (!passwordMatches)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is deactivated.");
        }

        // 3. Generate JWT containing required claims
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Role = user.Role,
            FullName = user.FullName,
            Email = user.Email,
            UserId = user.Id
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey == "SET_VIA_ENV_VAR_OR_SECRET")
        {
            jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
        }
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            jwtKey = "DevFallbackSecretKeyForLocalTestingOnly12345!";
        }

        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "EventCrew";
        if (string.IsNullOrWhiteSpace(jwtIssuer)) jwtIssuer = "EventCrew";

        var jwtAudience = _configuration["Jwt:Audience"] ?? "EventCrewUsers";
        if (string.IsNullOrWhiteSpace(jwtAudience)) jwtAudience = "EventCrewUsers";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new("role", user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
