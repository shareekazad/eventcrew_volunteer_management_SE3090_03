using EventCrew.Api.DTOs.Agent;
using EventCrew.Api.Services;
using EventCrew.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentController> _logger;
    private readonly AppDbContext _dbContext;

    public AgentController(IAgentService agentService, ILogger<AgentController> logger, AppDbContext dbContext)
    {
        _agentService = agentService;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Trigger the PlanningAgent for an event. Runs the full AI workflow:
    /// ASP.NET Core → Python AI service → tools → back → persisted state.
    /// </summary>
    [HttpPost("plan/{eventId:guid}")]
    [ProducesResponseType(typeof(PlanResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PlanResultDto>> Plan(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (!ResourceOwnership.IsAdmin(User))
        {
            var organizerId = ResourceOwnership.GetUserId(User);
            var ownsEvent = organizerId.HasValue && await _dbContext.Events.AsNoTracking()
                .AnyAsync(item => item.Id == eventId && item.OrganizerId == organizerId.Value, cancellationToken);
            if (!ownsEvent) return NotFound();
        }

        try
        {
            var plan = await _agentService.PlanStaffingAsync(eventId, cancellationToken);
            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Plan failed for event {EventId}", eventId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
