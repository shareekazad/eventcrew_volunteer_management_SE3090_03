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