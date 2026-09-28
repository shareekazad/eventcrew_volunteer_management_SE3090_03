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
        var shifts = await dbContext.Shifts
            .AsNoTracking()
            .OrderBy(shift => shift.StartTime)
            .ThenBy(shift => shift.Id)
            .Select(shift => ToResponse(shift))
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
        var shift = await dbContext.Shifts
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => ToResponse(candidate))
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
        var missingReference = await FindMissingReference(request.EventId, request.RoleRequirementId, cancellationToken);
        if (missingReference is not null)
        {
            return NotFound(missingReference);
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

        var response = ToResponse(shift);
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

        var missingReference = await FindMissingReference(request.EventId, request.RoleRequirementId, cancellationToken);
        if (missingReference is not null)
        {
            return NotFound(missingReference);
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

    private async Task<ProblemDetails?> FindMissingReference(
        Guid eventId,
        Guid roleRequirementId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Set<Event>().AnyAsync(eventEntity => eventEntity.Id == eventId, cancellationToken))
        {
            return CreateNotFoundProblem("Event", eventId);
        }

        if (!await dbContext.Set<RoleRequirement>().AnyAsync(requirement => requirement.Id == roleRequirementId, cancellationToken))
        {
            return CreateNotFoundProblem("Role requirement", roleRequirementId);
        }

        return null;
    }

    private static ProblemDetails CreateNotFoundProblem(string resource, Guid id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = $"{resource} not found",
        Detail = $"No {resource.ToLowerInvariant()} exists with identifier '{id}'."
    };

    private static ShiftResponse ToResponse(Shift shift) => new(
        shift.Id,
        shift.EventId,
        shift.RoleRequirementId,
        shift.Title,
        shift.StartTime,
        shift.EndTime,
        shift.Capacity,
        shift.Status,
        shift.CreatedAt,
        shift.UpdatedAt);
}