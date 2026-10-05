namespace EventCrew.Api.DTOs.Agent;

public class ValidationReportDto
{
    public bool IsValid { get; set; }
    public Guid EventId { get; set; }
    public int AssignmentsEvaluated { get; set; }
    public IReadOnlyList<ValidationIssueDto> Issues { get; set; } = Array.Empty<ValidationIssueDto>();
}

public class ValidationIssueDto
{
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? ShiftId { get; set; }
    public Guid? VolunteerId { get; set; }
}
