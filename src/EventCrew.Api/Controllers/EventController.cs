using EventCrew.Api.Dtos;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
[Route("api/events-lookup")]
[Produces("application/json")]
public sealed class EventController(EventCrewDbContext dbContext) : ControllerBase
{
    /// <summary>Gets events available for organizer shift management.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var eventsQuery = dbContext.Events.AsNoTracking();
        if (!ResourceOwnership.IsAdmin(User))
        {
            var organizerId = ResourceOwnership.GetUserId(User);
            if (organizerId is null) return Ok(Array.Empty<EventResponse>());
            eventsQuery = eventsQuery.Where(eventEntity => eventEntity.OrganizerId == organizerId.Value);
        }

        var events = await eventsQuery
            .OrderBy(eventEntity => eventEntity.Title)
            .Select(eventEntity => new EventResponse(eventEntity.Id, eventEntity.Title))
            .ToListAsync(cancellationToken);

        return Ok(events);
    }
}
