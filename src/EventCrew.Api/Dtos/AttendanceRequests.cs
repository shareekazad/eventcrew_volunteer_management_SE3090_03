using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.Dtos;

public sealed record AttendanceActionRequest : IValidatableObject
{
    [Required]
    public string Token { get; init; } = string.Empty;
    public Guid ShiftId { get; init; }
    public Guid VolunteerId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Token)) yield return new ValidationResult("Token is required.", [nameof(Token)]);
        if (ShiftId == Guid.Empty) yield return new ValidationResult("ShiftId is required.", [nameof(ShiftId)]);
        if (VolunteerId == Guid.Empty) yield return new ValidationResult("VolunteerId is required.", [nameof(VolunteerId)]);
    }
}

public sealed record QrTokenResponse(Guid Id, Guid ShiftId, string Token, DateTimeOffset ExpiresAt, bool IsActive, DateTimeOffset CreatedAt);
public sealed record AttendanceResponse(Guid Id, Guid ShiftAssignmentId, Guid ShiftId, string ShiftTitle, Guid VolunteerId, string VolunteerName, string VolunteerEmail, string Status, DateTimeOffset? CheckInTime, DateTimeOffset? CheckOutTime, decimal VerifiedHours, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
