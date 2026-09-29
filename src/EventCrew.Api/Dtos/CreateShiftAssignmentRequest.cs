using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.Dtos;

/// <summary>Volunteer profile and shift identifiers for an organizer assignment.</summary>
public sealed record CreateShiftAssignmentRequest : IValidatableObject
{
    public Guid ShiftId { get; init; }
    public Guid VolunteerId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ShiftId == Guid.Empty)
        {
            yield return new ValidationResult("ShiftId is required.", [nameof(ShiftId)]);
        }

        if (VolunteerId == Guid.Empty)
        {
            yield return new ValidationResult("VolunteerId is required.", [nameof(VolunteerId)]);
        }
    }
}