using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Shifts;

/// <summary>
/// Body for approving or rejecting a swap request.
/// Used by target volunteer (accept/decline) and organizer (approve/reject).
/// </summary>
public class UpdateSwapStatusDto
{
    /// <summary>
    /// For target volunteer: "Pending_Organizer" (accept) or "Rejected" (decline).
    /// For organizer: "Approved" or "Rejected".
    /// Requester may also send "Cancelled".
    /// </summary>
    [Required]
    [MaxLength(25)]
    public string Status { get; set; } = string.Empty;
}
