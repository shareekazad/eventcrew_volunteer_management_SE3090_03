using EventCrew.Api.DTOs.Venues;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    /// <summary>Get all venues.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VenueResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VenueResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var venues = await _venueService.GetAllAsync(cancellationToken);
        return Ok(venues);
    }

    /// <summary>Get a venue by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VenueResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VenueResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var venue = await _venueService.GetByIdAsync(id, cancellationToken);
        return venue is null ? NotFound() : Ok(venue);
    }

    /// <summary>Create a new venue.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VenueResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VenueResponseDto>> Create(
        [FromBody] CreateVenueDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _venueService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update an existing venue.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VenueResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VenueResponseDto>> Update(
        Guid id,
        [FromBody] UpdateVenueDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _venueService.UpdateAsync(id, dto, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Delete a venue.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _venueService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}