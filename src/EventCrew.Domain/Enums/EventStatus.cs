namespace EventCrew.Domain.Enums;

/// <summary>
/// Lifecycle status of an event. Matches the CHECK constraint in the DDL.
/// </summary>
public enum EventStatus
{
    Draft,
    Published,
    StaffingInProgress,
    FullyStaffed,
    Completed,
    Cancelled
}