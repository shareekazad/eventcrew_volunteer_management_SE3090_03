using EventCrew.Api.Dtos;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Authorize(Roles = AuthorizationRoles.All)]
[Route("api/role-requirements")]
[Produces("application/json")]
public sealed class RoleRequirementController(EventCrewDbContext dbContext) : ControllerBase
{
    /// <summary>Gets role requirements belonging to the selected event.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleRequirementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<RoleRequirementResponse>>> GetByEvent(
        [FromQuery] Guid eventId,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "EventId is required",
                Detail = "Provide a valid eventId query parameter."
            });
        }

        var eventQuery = dbContext.Events.AsNoTracking().Where(eventEntity => eventEntity.Id == eventId);
        if (ResourceOwnership.IsOrganizer(User))
        {
            var organizerId = ResourceOwnership.GetUserId(User);
            eventQuery = organizerId.HasValue
                ? eventQuery.Where(eventEntity => eventEntity.OrganizerId == organizerId.Value)
                : eventQuery.Where(_ => false);
        }
        if (!await eventQuery.AnyAsync(cancellationToken))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Event not found",
                Detail = $"No event exists with identifier '{eventId}'."
            });
        }

        var requirements = await dbContext.RoleRequirements
            .AsNoTracking()
            .Where(requirement => requirement.EventId == eventId)
            .OrderBy(requirement => requirement.RoleName)
            .Select(requirement => new RoleRequirementResponse(
                requirement.Id,
                requirement.EventId,
                requirement.RoleName,
                requirement.RequiredHeadcount))
            .ToListAsync(cancellationToken);

        return Ok(requirements);
    }
}
