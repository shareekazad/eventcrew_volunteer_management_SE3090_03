using EventCrew.Api.Dtos;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
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

        if (!await dbContext.Events.AnyAsync(eventEntity => eventEntity.Id == eventId, cancellationToken))
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