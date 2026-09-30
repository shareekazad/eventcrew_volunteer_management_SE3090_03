namespace EventCrew.Domain.Entities;

public class QrCodeToken
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public Shift Shift { get; set; } = null!;
}
