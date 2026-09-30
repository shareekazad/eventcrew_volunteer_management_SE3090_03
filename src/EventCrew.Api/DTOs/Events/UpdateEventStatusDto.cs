using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Events;

/// <summary>
/// Request shape for a status transition on an event.
/// Only the new status is accepted — everything else is immutable here.
/// </summary>
public class UpdateEventStatusDto
{
    [Required(ErrorMessage = "New status is required.")]
    [RegularExpression(
        "^(Draft|Published|StaffingInProgress|FullyStaffed|Completed|Cancelled)$",
        ErrorMessage = "Status must be one of: Draft, Published, StaffingInProgress, FullyStaffed, Completed, Cancelled.")]
    public string NewStatus { get; set; } = string.Empty;
}