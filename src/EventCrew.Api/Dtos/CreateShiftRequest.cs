using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.Dtos;

/// <summary>Request body used to create a shift.</summary>
public sealed record CreateShiftRequest : IValidatableObject
{
    /// <summary>Shift display title.</summary>
    [Required, StringLength(150)]
    public string? Title { get; init; }

    /// <summary>Identifier of the event that owns this shift.</summary>
    [Required]
    public Guid EventId { get; init; }

    /// <summary>Identifier of the required role requirement.</summary>
    [Required]
    public Guid RoleRequirementId { get; init; }

    /// <summary>Shift start time, including its UTC offset.</summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>Shift end time, including its UTC offset.</summary>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>Maximum number of volunteers for the shift.</summary>
    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EventId == Guid.Empty)
        {
            yield return new ValidationResult("EventId is required.", [nameof(EventId)]);
        }

        if (RoleRequirementId == Guid.Empty)
        {
            yield return new ValidationResult("RoleRequirementId is required.", [nameof(RoleRequirementId)]);
        }

        if (StartTime >= EndTime)
        {
            yield return new ValidationResult(
                "StartTime must be earlier than EndTime.",
                [nameof(StartTime), nameof(EndTime)]);
        }
    }
}