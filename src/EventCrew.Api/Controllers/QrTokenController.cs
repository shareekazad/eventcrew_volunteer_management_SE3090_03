using System.Security.Cryptography;
using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
[Route("api/shifts/{shiftId:guid}/qr-tokens")]
[Produces("application/json")]
public sealed class QrTokenController(EventCrewDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<QrTokenResponse>> Create(Guid shiftId, CancellationToken cancellationToken)
    {
        var shift = await dbContext.Shifts.Include(item => item.Event)
            .SingleOrDefaultAsync(item => item.Id == shiftId, cancellationToken);
        if (shift is null || !ResourceOwnership.CanManageEvent(User, shift.Event.OrganizerId))
            return NotFound(new ProblemDetails { Status = 404, Title = "Shift not found", Detail = $"No shift exists with identifier '{shiftId}'." });
        var now = DateTimeOffset.UtcNow;
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var existing = await dbContext.QrCodeTokens.Where(token => token.ShiftId == shiftId && token.IsActive).ToListAsync(cancellationToken);
        foreach (var item in existing) item.IsActive = false;
        var entity = new QrCodeToken { Id = Guid.NewGuid(), ShiftId = shiftId, TokenHash = AttendanceController.HashToken(rawToken), ExpiresAt = now.AddHours(8), IsActive = true, CreatedAt = now };
        dbContext.QrCodeTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Create), new { shiftId }, new QrTokenResponse(entity.Id, shiftId, rawToken, entity.ExpiresAt, entity.IsActive, entity.CreatedAt));
    }
}
