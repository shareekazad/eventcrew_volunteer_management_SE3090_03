namespace EventCrew.Api.DTOs.Agent;

/// <summary>
/// Minimal response returned when a workflow run is created.
/// The client uses the RunId to poll the run status or approve/reject it.
/// </summary>
public class WorkflowRunStatusDto
{
    public Guid RunId { get; set; }
    public Guid EventId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}