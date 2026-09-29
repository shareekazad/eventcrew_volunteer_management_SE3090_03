namespace EventCrew.Api.Dtos;

/// <summary>Shift representation returned by the API.</summary>
public sealed record ShiftResponse(
    Guid Id,
    Guid EventId,
    Guid RoleRequirementId,
    string Title,
    string EventName,
    string RoleRequirementName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int Capacity,
    int AssignedCount,
    int RemainingCapacity,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);