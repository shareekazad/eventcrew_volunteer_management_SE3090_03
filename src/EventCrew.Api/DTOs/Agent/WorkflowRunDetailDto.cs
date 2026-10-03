namespace EventCrew.Api.DTOs.Agent;

/// <summary>
/// Full details of a workflow run — used by the organizer dashboard
/// to view the plan, status, approval info, and audit trail.
/// </summary>
public class WorkflowRunDetailDto
{
    public Guid RunId { get; set; }
    public Guid EventId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;

    /// <summary>
    /// The full plan as a JSON string. Deserializing this on the client
    /// gives back the same shape as PlanResultDto.
    /// </summary>
    public string? PlanSummary { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}