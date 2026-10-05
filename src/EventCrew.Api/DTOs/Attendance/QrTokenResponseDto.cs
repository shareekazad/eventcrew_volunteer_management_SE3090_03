namespace EventCrew.Api.DTOs.Attendance;

public class QrTokenResponseDto
{
    public Guid ShiftId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
