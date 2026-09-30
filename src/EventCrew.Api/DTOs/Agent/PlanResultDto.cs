using System.Text.Json.Serialization;

namespace EventCrew.Api.DTOs.Agent;

/// <summary>
/// Response returned from the Python AI service — a structured staffing plan.
/// Mirrors the PlanResult model in the Python PlanningAgent.
/// </summary>
public class PlanResultDto
{
    [JsonPropertyName("objective")]
    public string Objective { get; set; } = string.Empty;

    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<PlanStepDto> Steps { get; set; } = new();

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    [JsonPropertyName("tool_calls")]
    public List<ToolCallDto> ToolCalls { get; set; } = new();

    [JsonPropertyName("next_agent")]
    public string NextAgent { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}


/// <summary>One step of the plan.</summary>
public class PlanStepDto
{
    [JsonPropertyName("step_number")]
    public int StepNumber { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("tool")]
    public string? Tool { get; set; }

    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}


/// <summary>An audit log entry for a single tool call.</summary>
public class ToolCallDto
{
    [JsonPropertyName("tool_name")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("input_params")]
    public Dictionary<string, object> InputParams { get; set; } = new();

    [JsonPropertyName("output_summary")]
    public string OutputSummary { get; set; } = string.Empty;

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; set; }

    [JsonPropertyName("called_at")]
    public string CalledAt { get; set; } = string.Empty;
}