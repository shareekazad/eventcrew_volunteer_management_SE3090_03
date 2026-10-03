using EventCrew.Api.Dtos;
using EventCrew.Api.Services;
using EventCrew.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    AppDbContext dbContext,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Email.ToUpper() == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || !passwordService.VerifyPassword(user, request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "The email or password is incorrect."
            });
        }

        var token = jwtTokenService.CreateAccessToken(user);
        return Ok(new LoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            new AuthenticatedUserResponse(user.Id, user.FullName, user.Email, user.Role)));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedUserResponse>> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized();

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null || !user.IsActive) return Unauthorized();

        return Ok(new AuthenticatedUserResponse(user.Id, user.FullName, user.Email, user.Role));
    }
}
