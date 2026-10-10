using EventCrew.Api.DTOs;
using EventCrew.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

/// <summary>
/// Read-only catalog of all available skills.
/// Used by the volunteer profile screen to populate the skill picker.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SkillsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SkillsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Get all skills in the catalog, ordered by category then name.</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<SkillResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var skills = await _db.Skills
            .AsNoTracking()
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new SkillResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
            })
            .ToListAsync();

        return Ok(skills);
    }
}