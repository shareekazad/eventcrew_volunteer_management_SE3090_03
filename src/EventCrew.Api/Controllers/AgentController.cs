using EventCrew.Api.DTOs.Agent;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(IAgentService agentService, ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _logger = logger;
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