namespace EventCrew.Api.Dtos;

/// <summary>Volunteer who may be assigned to the selected shift.</summary>
public sealed record EligibleVolunteerResponse(Guid Id, string FullName, string Email);