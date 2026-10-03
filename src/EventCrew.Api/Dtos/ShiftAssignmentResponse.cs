namespace EventCrew.Api.Dtos;

/// <summary>Assignment and minimal volunteer/shift details for organizer views.</summary>
public sealed record ShiftAssignmentResponse(
    Guid Id,
    Guid ShiftId,
    string ShiftTitle,
    Guid EventId,
    string EventName,
    Guid VolunteerId,
    string VolunteerName,
    string VolunteerEmail,
    string Status,
    DateTimeOffset AssignedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Assignments and volunteer profile ID for the signed-in volunteer.</summary>
public sealed record VolunteerAssignmentsResponse(
    Guid VolunteerId,
    IReadOnlyList<MyShiftAssignmentResponse> Assignments);

/// <summary>Minimal assignment details used by the volunteer mobile experience.</summary>
public sealed record MyShiftAssignmentResponse(
    Guid AssignmentId,
    Guid ShiftId,
    string Title,
    string EventName,
    string RoleRequirementName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status);