using EventCrew.Api.DTOs.Agent;

namespace EventCrew.Api.Services;

/// <summary>
/// Contract for calling the Python AI service, persisting the workflow run
/// and its tool-call log, and returning the structured plan.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Runs the PlanningAgent for the given event:
    /// 1. Calls the Python AI service.
    /// 2. Persists an AgentWorkflowRun row (workflow state).
    /// 3. Persists AgentToolLog rows (audit trail).
    /// 4. Returns the plan.
    /// </summary>
    Task<PlanResultDto> PlanStaffingAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a volunteer matching request to the Python AI microservice.
    /// React MUST call only this endpoint — never the Python service directly.
    /// Returns a graceful fallback with sample data if the Python service is offline.
    /// </summary>
    Task<MatchingResponseDto> MatchVolunteersAsync(MatchingRequestDto request, Guid initiatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Human-in-the-Loop approval: persists the AI proposal as accepted,
    /// transitions matched candidates to Shortlisted status in the database.
    /// </summary>
    Task<MatchingResponseDto> ApproveMatchingAsync(ApproveMatchingRequestDto request, Guid reviewerUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Human-in-the-Loop rejection: records reviewer feedback notes and
    /// marks the workflow run as Rejected.
    /// </summary>
    Task<MatchingResponseDto> RejectMatchingAsync(RejectMatchingRequestDto request, Guid reviewerUserId, CancellationToken cancellationToken = default);
}