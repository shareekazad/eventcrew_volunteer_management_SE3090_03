using EventCrew.Api.DTOs.Agent;

namespace EventCrew.Api.Services;

/// <summary>
/// Contract for calling the Python AI service, persisting the workflow run
/// and its tool-call log, and managing human approval of high-impact actions.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Runs the PlanningAgent for the given event:
    /// 1. Calls the Python AI service.
    /// 2. Persists an AgentWorkflowRun row with status = "AwaitingApproval".
    /// 3. Persists AgentToolLog rows (audit trail).
    /// 4. Returns the run ID + status.
    ///
    /// The plan is NOT auto-approved — an organizer must call ApproveAsync
    /// or RejectAsync.
    /// </summary>
    Task<WorkflowRunStatusDto> PlanStaffingAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full state of a workflow run (plan, status, audit info).
    /// </summary>
    Task<WorkflowRunDetailDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a workflow run that is in AwaitingApproval state.
    /// Returns the updated run, or null if not found.
    /// Throws InvalidOperationException if the run is not awaiting approval.
    /// </summary>
    Task<WorkflowRunDetailDto?> ApproveAsync(Guid runId, Guid reviewedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a workflow run that is in AwaitingApproval state, with a required reason.
    /// Returns the updated run, or null if not found.
    /// Throws InvalidOperationException if the run is not awaiting approval.
    /// </summary>
    Task<WorkflowRunDetailDto?> RejectAsync(Guid runId, Guid reviewedByUserId, string reason, CancellationToken cancellationToken = default);
}