using EventCrew.Api.Dtos;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public sealed class EventController(EventCrewDbContext dbContext) : ControllerBase
{
    /// <summary>Gets events available for organizer shift management.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var events = await dbContext.Events
            .AsNoTracking()
            .OrderBy(eventEntity => eventEntity.Title)
            .Select(eventEntity => new EventResponse(eventEntity.Id, eventEntity.Title))
            .ToListAsync(cancellationToken);

        return Ok(events);
    }
}