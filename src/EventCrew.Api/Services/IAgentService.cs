using EventCrew.Api.DTOs.Agent;

namespace EventCrew.Api.Services;

/// <summary>
/// Contract for calling the Python AI service, persisting the workflow run
/// and its tool-call log, and managing human approval of high-impact actions.
/// </summary>
public interface IAgentService
{
    // ============================================================
    // Planning workflow (Student 1)
    // ============================================================

    /// <summary>
    /// Runs the PlanningAgent for the given event, persists an AgentWorkflowRun
    /// in "AwaitingApproval" status, and returns the run ID + status.
    /// The plan is NOT auto-approved — an organizer must call ApproveAsync or RejectAsync.
    /// </summary>
    Task<WorkflowRunStatusDto> PlanStaffingAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the full state of a workflow run (plan, status, audit info).</summary>
    Task<WorkflowRunDetailDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>Approves a workflow run that is in AwaitingApproval state.</summary>
    Task<WorkflowRunDetailDto?> ApproveAsync(Guid runId, Guid reviewedByUserId, CancellationToken cancellationToken = default);

    /// <summary>Rejects a workflow run that is in AwaitingApproval state, with a required reason.</summary>
    Task<WorkflowRunDetailDto?> RejectAsync(Guid runId, Guid reviewedByUserId, string reason, CancellationToken cancellationToken = default);

    // ============================================================
    // Volunteer matching workflow (Student 2)
    // ============================================================

    /// <summary>
    /// Proxies a volunteer matching request to the Python AI microservice.
    /// Returns a graceful fallback with sample data if the Python service is offline.
    /// </summary>
    Task<MatchingResponseDto> MatchVolunteersAsync(MatchingRequestDto request, Guid initiatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>Human-in-the-Loop approval for a matching proposal.</summary>
    Task<MatchingResponseDto> ApproveMatchingAsync(ApproveMatchingRequestDto request, Guid reviewerUserId, CancellationToken cancellationToken = default);

    /// <summary>Human-in-the-Loop rejection for a matching proposal.</summary>
    Task<MatchingResponseDto> RejectMatchingAsync(RejectMatchingRequestDto request, Guid reviewerUserId, CancellationToken cancellationToken = default);
}