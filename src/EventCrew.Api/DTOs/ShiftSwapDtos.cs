using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.Dtos;

public sealed record CreateShiftSwapRequest : IValidatableObject
{
    [Required]
    public Guid RequesterAssignmentId { get; init; }

    [Required]
    public Guid TargetVolunteerId { get; init; }

    [Required]
    public Guid TargetShiftId { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RequesterAssignmentId == Guid.Empty)
        {
            yield return new ValidationResult("RequesterAssignmentId is required.", [nameof(RequesterAssignmentId)]);
        }

        if (TargetVolunteerId == Guid.Empty)
        {
            yield return new ValidationResult("TargetVolunteerId is required.", [nameof(TargetVolunteerId)]);
        }

        if (TargetShiftId == Guid.Empty)
        {
            yield return new ValidationResult("TargetShiftId is required.", [nameof(TargetShiftId)]);
        }
    }
}

public sealed record ShiftSwapResponse(
    Guid Id,
    Guid RequesterAssignmentId,
    Guid RequesterVolunteerId,
    string RequesterName,
    string RequesterEmail,
    Guid SourceShiftId,
    string SourceShiftTitle,
    Guid SourceEventId,
    string SourceEventTitle,
    Guid TargetVolunteerId,
    string TargetVolunteerName,
    string TargetVolunteerEmail,
    Guid TargetShiftId,
    string TargetShiftTitle,
    Guid TargetEventId,
    string TargetEventTitle,
    string? Reason,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
