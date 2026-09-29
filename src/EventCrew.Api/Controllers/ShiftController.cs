using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/shifts")]
[Produces("application/json")]
public sealed class ShiftController(EventCrewDbContext dbContext) : ControllerBase
{
    /// <summary>Gets all shifts.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ShiftResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var shifts = await ProjectShifts(dbContext.Shifts.AsNoTracking())
            .OrderBy(shift => shift.StartTime)
            .ThenBy(shift => shift.Id)
            .ToListAsync(cancellationToken);

        return Ok(shifts);
    }

    /// <summary>Gets a shift by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShiftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var shift = await ProjectShifts(dbContext.Shifts.AsNoTracking())
            .Where(candidate => candidate.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        return shift is null ? NotFound(CreateNotFoundProblem("Shift", id)) : Ok(shift);
    }

    /// <summary>Creates a shift.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShiftResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftResponse>> Create(
        CreateShiftRequest request,
        CancellationToken cancellationToken)
    {
        var invalidReference = await ValidateReferences(request.EventId, request.RoleRequirementId, cancellationToken);
        if (invalidReference is not null)
        {
            return invalidReference;
        }

        var now = DateTimeOffset.UtcNow;
        var shift = new Shift
        {
            EventId = request.EventId,
            RoleRequirementId = request.RoleRequirementId,
            Title = request.Title!,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Capacity = request.Capacity,
            Status = "Scheduled",
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await ProjectShifts(dbContext.Shifts.AsNoTracking())
            .Where(candidate => candidate.Id == shift.Id)
            .SingleAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = shift.Id }, response);
    }

    /// <summary>Updates all editable fields of a shift.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateShiftRequest request,
        CancellationToken cancellationToken)
    {
        var shift = await dbContext.Shifts.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (shift is null)
        {
            return NotFound(CreateNotFoundProblem("Shift", id));
        }

        var invalidReference = await ValidateReferences(request.EventId, request.RoleRequirementId, cancellationToken);
        if (invalidReference is not null)
        {
            return invalidReference;
        }

        shift.EventId = request.EventId;
        shift.RoleRequirementId = request.RoleRequirementId;
        shift.Title = request.Title!;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.Capacity = request.Capacity;
        shift.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Deletes a shift.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var shift = await dbContext.Shifts.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (shift is null)
        {
            return NotFound(CreateNotFoundProblem("Shift", id));
        }

        dbContext.Shifts.Remove(shift);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateReferences(
        Guid eventId,
        Guid roleRequirementId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Events.AnyAsync(eventEntity => eventEntity.Id == eventId, cancellationToken))
        {
            return NotFound(CreateNotFoundProblem("Event", eventId));
        }

        var requirement = await dbContext.RoleRequirements
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == roleRequirementId, cancellationToken);
        if (requirement is null)
        {
            return NotFound(CreateNotFoundProblem("Role requirement", roleRequirementId));
        }
        if (!requirement.BelongsToEvent(eventId))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(CreateShiftRequest.RoleRequirementId)] = ["The role requirement must belong to the selected event."]
            }));
        }

        return null;
    }

    private static IQueryable<ShiftResponse> ProjectShifts(IQueryable<Shift> shifts) =>
        shifts.Select(shift => new
            {
                Shift = shift,
                AssignedCount = shift.Assignments.Count(assignment => assignment.Status == "Confirmed" || assignment.Status == "Completed")
            })
            .Select(result => new ShiftResponse(
                result.Shift.Id,
                result.Shift.EventId,
                result.Shift.RoleRequirementId,
                result.Shift.Title,
                result.Shift.Event.Title,
                result.Shift.RoleRequirement.RoleName,
                result.Shift.StartTime,
                result.Shift.EndTime,
                result.Shift.Capacity,
                result.AssignedCount,
                result.AssignedCount >= result.Shift.Capacity ? 0 : result.Shift.Capacity - result.AssignedCount,
                result.Shift.Status,
                result.Shift.CreatedAt,
                result.Shift.UpdatedAt));

    private static ProblemDetails CreateNotFoundProblem(string resource, Guid id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = $"{resource} not found",
        Detail = $"No {resource.ToLowerInvariant()} exists with identifier '{id}'."
    };

}