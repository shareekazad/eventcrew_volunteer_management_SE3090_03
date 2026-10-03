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

    public AgentController(
        IAgentService agentService,
        ILogger<AgentController> logger,
        AppDbContext dbContext)
    {
        _agentService = agentService;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>Runs the planning graph and pauses for organizer/admin review.</summary>
    [HttpPost("plan/{eventId:guid}")]
    [ProducesResponseType(typeof(WorkflowRunStatusDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WorkflowRunStatusDto>> Plan(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventOrganizerId = await _dbContext.Events.AsNoTracking()
            .Where(item => item.Id == eventId)
            .Select(item => (Guid?)item.OrganizerId)
            .FirstOrDefaultAsync(cancellationToken);

        var initiatedByUserId = ResourceOwnership.GetUserId(User);
        if (eventOrganizerId is null ||
            (!ResourceOwnership.IsAdmin(User) && eventOrganizerId != initiatedByUserId))
        {
            return NotFound();
        }

        if (initiatedByUserId is null)
        {
            return Forbid();
        }

        try
        {
            var result = await _agentService.PlanStaffingAsync(
                eventId,
                initiatedByUserId.Value,
                cancellationToken);
            return Accepted(result);
        }
        catch (AiServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "Planning service failed for event {EventId}", eventId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Planning request rejected for event {EventId}", eventId);
            return BadRequest(new { error = ex.Message });
        }
    }
    // ============================================================
    // 2. GET RUN — full details of a workflow run
    // ============================================================
    /// <summary>Gets the workflow run, persisted plan, and tool audit log.</summary>
    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType(typeof(WorkflowRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowRunDetailDto>> GetRun(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var run = await _agentService.GetRunAsync(runId, cancellationToken);
        if (run is null || !await CanAccessRunAsync(run, cancellationToken))
        {
            return NotFound();
        }

        return Ok(run);
    }

    // ============================================================
    // 3. APPROVE — organizer approves the run
    // ============================================================
    /// <summary>Approves a workflow run that is awaiting review.</summary>
    [HttpPost("runs/{runId:guid}/approve")]
    [ProducesResponseType(typeof(WorkflowRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowRunDetailDto>> Approve(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var reviewedByUserId = ResourceOwnership.GetUserId(User);
        if (reviewedByUserId is null)
        {
            return Forbid();
        }

        var run = await _agentService.GetRunAsync(runId, cancellationToken);
        if (run is null || !await CanAccessRunAsync(run, cancellationToken))
        {
            return NotFound();
        }

        try
        {
            var updated = await _agentService.ApproveAsync(runId, reviewedByUserId.Value, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Approval failed for run {RunId}", runId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================
    // 4. REJECT — organizer rejects the run with a reason
    // ============================================================
    /// <summary>Rejects a workflow run that is awaiting review, with an audit reason.</summary>
    [HttpPost("runs/{runId:guid}/reject")]
    [ProducesResponseType(typeof(WorkflowRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowRunDetailDto>> Reject(
        Guid runId,
        [FromBody] RejectRunDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new { error = "A rejection reason is required." });
        }

        var reviewedByUserId = ResourceOwnership.GetUserId(User);
        if (reviewedByUserId is null)
        {
            return Forbid();
        }

        var run = await _agentService.GetRunAsync(runId, cancellationToken);
        if (run is null || !await CanAccessRunAsync(run, cancellationToken))
        {
            return NotFound();
        }

        try
        {
            var updated = await _agentService.RejectAsync(
                runId,
                reviewedByUserId.Value,
                dto.Reason.Trim(),
                cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rejection failed for run {RunId}", runId);
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<bool> CanAccessRunAsync(
        WorkflowRunDetailDto run,
        CancellationToken cancellationToken)
    {
        if (ResourceOwnership.IsAdmin(User))
        {
            return true;
        }

        var organizerId = ResourceOwnership.GetUserId(User);
        return organizerId.HasValue && await _dbContext.Events.AsNoTracking()
            .AnyAsync(
                item => item.Id == run.EventId && item.OrganizerId == organizerId.Value,
                cancellationToken);
    }
}
