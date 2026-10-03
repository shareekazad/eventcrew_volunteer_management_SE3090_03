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

    // Temporary: reviewer ID until JWT claims are wired in.
    // Once JWT is fully set up, this comes from HttpContext.User.
    // For now, we accept it from a header X-Reviewer-Id for testing.
    private const string ReviewerHeaderName = "X-Reviewer-Id";

    public AgentController(IAgentService agentService, ILogger<AgentController> logger, AppDbContext dbContext)
    {
        _agentService = agentService;
        _logger = logger;
        _dbContext = dbContext;
    }

    // ============================================================
    // 1. PLAN — runs the workflow, returns AwaitingApproval
    // ============================================================
    /// <summary>
    /// Trigger the PlanningAgent for an event. The workflow runs and pauses
    /// in AwaitingApproval status until an organizer approves or rejects it.
    /// Returns 202 Accepted with the run ID.
    /// </summary>
    [HttpPost("plan/{eventId:guid}")]
    [ProducesResponseType(typeof(WorkflowRunStatusDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WorkflowRunStatusDto>> Plan(
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
            var result = await _agentService.PlanStaffingAsync(eventId, cancellationToken);
            return Accepted(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Plan failed for event {EventId}", eventId);
            return BadRequest(new { error = ex.Message });
        }
    }
    // ============================================================
    // 2. GET RUN — full details of a workflow run
    // ============================================================
    /// <summary>Get the current state of a workflow run (status, plan, review info).</summary>
    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType(typeof(WorkflowRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowRunDetailDto>> GetRun(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var run = await _agentService.GetRunAsync(runId, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    // ============================================================
    // 3. APPROVE — organizer approves the run
    // ============================================================
    /// <summary>
    /// Approve a workflow run that is in AwaitingApproval status.
    /// The reviewer ID is read from the X-Reviewer-Id header until JWT claims are wired.
    /// </summary>
    [HttpPost("runs/{runId:guid}/approve")]
    [ProducesResponseType(typeof(WorkflowRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowRunDetailDto>> Approve(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var reviewerId = ReadReviewerId();
        if (reviewerId is null)
            return BadRequest(new { error = $"Missing or invalid '{ReviewerHeaderName}' header (must be a GUID)." });

        try
        {
            var updated = await _agentService.ApproveAsync(runId, reviewerId.Value, cancellationToken);
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
    /// <summary>
    /// Reject a workflow run that is in AwaitingApproval status, with a required reason.
    /// The reviewer ID is read from the X-Reviewer-Id header until JWT claims are wired.
    /// </summary>
    [HttpPost("runs/{runId:guid}/reject")]
    [ProducesResponseType(typeof(WorkflowRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowRunDetailDto>> Reject(
        Guid runId,
        [FromBody] RejectRunDto dto,
        CancellationToken cancellationToken)
    {
        var reviewerId = ReadReviewerId();
        if (reviewerId is null)
            return BadRequest(new { error = $"Missing or invalid '{ReviewerHeaderName}' header (must be a GUID)." });

        try
        {
            var updated = await _agentService.RejectAsync(runId, reviewerId.Value, dto.Reason, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rejection failed for run {RunId}", runId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================
    // HELPER
    // ============================================================
    private Guid? ReadReviewerId()
    {
        if (!Request.Headers.TryGetValue(ReviewerHeaderName, out var values))
            return null;

        var raw = values.ToString();
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
