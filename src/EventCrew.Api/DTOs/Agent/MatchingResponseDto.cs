using System.Text.Json.Serialization;

namespace EventCrew.Api.DTOs.Agent;

/// <summary>
/// Full matching response returned by the Python AI microservice and forwarded to React.
/// </summary>
public class MatchingResponseDto
{
    [JsonPropertyName("workflow_id")]
    public Guid WorkflowId { get; set; }

    [JsonPropertyName("role_name")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("headcount_needed")]
    public int HeadcountNeeded { get; set; }

    [JsonPropertyName("matched_candidates")]
    public List<CandidateMatchDto> MatchedCandidates { get; set; } = new();

    [JsonPropertyName("unfulfilled_slots")]
    public int UnfulfilledSlots { get; set; }

    [JsonPropertyName("execution_time_ms")]
    public int ExecutionTimeMs { get; set; }

    /// <summary>SUCCESS | PARTIAL_MATCH | SAFE_FAILURE</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    // Enrichment: set by the gateway after persisting the workflow run
    [JsonPropertyName("workflow_run_id")]
    public Guid? WorkflowRunId { get; set; }

    // Graceful fallback: populated when Python service is offline
    [JsonPropertyName("is_fallback")]
    public bool IsFallback { get; set; }
}

/// <summary>
/// A single ranked candidate produced by the AI scoring algorithm.
/// </summary>
public class CandidateMatchDto
{
    [JsonPropertyName("volunteer_id")]
    public Guid VolunteerId { get; set; }

    [JsonPropertyName("volunteer_name")]
    public string VolunteerName { get; set; } = string.Empty;

    [JsonPropertyName("match_score")]
    public double MatchScore { get; set; }

    [JsonPropertyName("matching_skills")]
    public List<string> MatchingSkills { get; set; } = new();

    [JsonPropertyName("experience_level")]
    public string ExperienceLevel { get; set; } = string.Empty;

    [JsonPropertyName("rating_score")]
    public double RatingScore { get; set; }

    [JsonPropertyName("justification")]
    public string Justification { get; set; } = string.Empty;
}

/// <summary>
/// Request body for the Human-in-the-Loop approval step.
/// </summary>
public class ApproveMatchingRequestDto
{
    public Guid WorkflowRunId { get; set; }
    public string? OrganizerNotes { get; set; }
}

/// <summary>
/// Request body for the Human-in-the-Loop rejection step.
/// </summary>
public class RejectMatchingRequestDto
{
    public Guid WorkflowRunId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
