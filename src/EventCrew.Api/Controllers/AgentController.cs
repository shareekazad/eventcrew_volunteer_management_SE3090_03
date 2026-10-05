using EventCrew.Api.DTOs.Agent;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentController> _logger;

    private const string ReviewerHeaderName = "X-Reviewer-Id";

    public AgentController(IAgentService agentService, ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    // ============================================================
    // 1. PLAN — runs workflow, returns AwaitingApproval
    // ============================================================
    [HttpPost("plan/{eventId:guid}")]
    [ProducesResponseType(typeof(WorkflowRunStatusDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WorkflowRunStatusDto>> Plan(
        Guid eventId,
        CancellationToken cancellationToken)
    {
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
    // 5. MATCHING — Volunteer matching gateway (Student 2)
    // ============================================================
    [HttpPost("match-volunteers")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(MatchingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MatchingResponseDto>> MatchVolunteers(
        [FromBody] MatchingRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var initiatorId = GetCurrentUserId();

        try
        {
            _logger.LogInformation(
                "Organizer {UserId} triggered volunteer matching for event {EventId}, role '{Role}'",
                initiatorId, request.EventId, request.RoleName);

            var result = await _agentService.MatchVolunteersAsync(request, initiatorId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Volunteer matching failed for event {EventId}", request.EventId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Volunteer matching encountered an internal error.", detail = ex.Message });
        }
    }

    [HttpPost("match-volunteers/approve")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(MatchingResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchingResponseDto>> ApproveMatchingProposal(
        [FromBody] ApproveMatchingRequestDto request,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId();
        _logger.LogInformation(
            "Organizer {UserId} approving AI matching workflow {RunId}",
            reviewerId, request.WorkflowRunId);

        var result = await _agentService.ApproveMatchingAsync(request, reviewerId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("match-volunteers/reject")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(MatchingResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchingResponseDto>> RejectMatchingProposal(
        [FromBody] RejectMatchingRequestDto request,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId();
        _logger.LogInformation(
            "Organizer {UserId} rejecting AI matching workflow {RunId}: {Reason}",
            reviewerId, request.WorkflowRunId, request.Reason);

        var result = await _agentService.RejectMatchingAsync(request, reviewerId, cancellationToken);
        return Ok(result);
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private Guid? ReadReviewerId()
    {
        if (!Request.Headers.TryGetValue(ReviewerHeaderName, out var values))
            return null;

        var raw = values.ToString();
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(sub, out var userId)
            ? userId
            : Guid.Parse("a0000000-0000-0000-0000-000000000001");
    }
}