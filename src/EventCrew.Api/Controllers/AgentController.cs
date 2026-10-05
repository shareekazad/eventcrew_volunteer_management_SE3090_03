using EventCrew.Api.DTOs.Agent;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/agents")]
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

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/Agent/match-volunteers & /api/agents/match-volunteers
    // AI Matching Gateway — React → ASP.NET Core → Python AI Microservice
    // Architecture Rule: React NEVER calls Python directly.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gateway: Receives a matching request from the React Organizer Hub,
    /// proxies it to the internal Python Volunteer Matching Agent at
    /// http://localhost:8000/api/agents/match-volunteers, persists the
    /// workflow run, and returns the ranked candidate result.
    ///
    /// Falls back to realistic sample data when the Python service is offline.
    /// </summary>
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

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/Agent/match-volunteers/approve & /api/agents/match-volunteers/approve
    // Human-in-the-Loop (HITL) Approval Step — Section 9.1 & 10
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Human-in-the-Loop approval: the organizer accepts the AI-proposed roster.
    /// Marks the workflow run as Approved and persists the reviewer's decision.
    /// </summary>
    [HttpPost("match-volunteers/approve")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(MatchingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/Agent/match-volunteers/reject & /api/agents/match-volunteers/reject
    // Human-in-the-Loop (HITL) Rejection Step — Section 9.1 & 10
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Human-in-the-Loop rejection: the organizer rejects the AI-proposed roster.
    /// Records the rejection reason and marks the workflow run as Rejected.
    /// </summary>
    [HttpPost("match-volunteers/reject")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(MatchingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

    // ── Private Helpers ────────────────────────────────────────────────────
    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        return Guid.TryParse(sub, out var userId)
            ? userId
            : Guid.Parse("a0000000-0000-0000-0000-000000000001"); // demo organizer fallback
    }
}