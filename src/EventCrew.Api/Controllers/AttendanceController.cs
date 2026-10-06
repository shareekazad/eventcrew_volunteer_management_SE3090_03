using EventCrew.Api.DTOs.Attendance;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("qr-tokens")]
    [ProducesResponseType(typeof(QrTokenResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QrTokenResponseDto>> CreateQrToken(
        [FromBody] CreateQrTokenRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _attendanceService.CreateQrTokenAsync(dto, cancellationToken);
            return token is null ? NotFound() : StatusCode(StatusCodes.Status201Created, token);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("check-in")]
    [ProducesResponseType(typeof(AttendanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponseDto>> CheckIn(
        [FromBody] CheckInRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _attendanceService.CheckInAsync(dto, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Event or shift does not exist." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{attendanceId:guid}/check-out")]
    [ProducesResponseType(typeof(AttendanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponseDto>> CheckOut(
        Guid attendanceId,
        [FromBody] CheckOutRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var attendance = await _attendanceService.CheckOutAsync(
                attendanceId,
                dto,
                cancellationToken);
            return attendance is null ? NotFound() : Ok(attendance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AttendanceResponseDto>>> GetAttendance(
        [FromQuery] Guid? eventId,
        [FromQuery] Guid? shiftId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        try
        {
            var attendance = await _attendanceService.GetAttendanceAsync(
                eventId,
                shiftId,
                status,
                cancellationToken);
            return attendance is null ? NotFound() : Ok(attendance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AttendanceStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceStatisticsDto>> GetStatistics(
        [FromQuery] Guid eventId,
        [FromQuery] Guid? shiftId,
        CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _attendanceService.GetStatisticsAsync(
                eventId,
                shiftId,
                cancellationToken);
            return statistics is null ? NotFound() : Ok(statistics);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("volunteers/{volunteerId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AttendanceResponseDto>>> GetVolunteerHistory(
        Guid volunteerId,
        CancellationToken cancellationToken)
    {
        var history = await _attendanceService.GetVolunteerHistoryAsync(volunteerId, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }
}
