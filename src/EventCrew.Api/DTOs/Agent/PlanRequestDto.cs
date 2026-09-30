using System.Text.Json.Serialization;

namespace EventCrew.Api.DTOs.Agent;

/// <summary>
/// Request sent to the Python AI service when asking for a staffing plan.
/// Uses snake_case to match the Python Pydantic model.
/// </summary>
public class PlanRequestDto
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;
}