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
}