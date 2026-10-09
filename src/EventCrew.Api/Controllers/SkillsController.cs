using EventCrew.Api.DTOs;
using EventCrew.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCrew.Api.Controllers;

/// <summary>
/// Exposes available skills in the platform.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SkillsController : ControllerBase
{
    private readonly IVolunteerService _volunteerService;

    public SkillsController(IVolunteerService volunteerService)
    {
        _volunteerService = volunteerService;
    }

    /// <summary>Retrieve all platform skills for interactive skill selection.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSkills()
    {
        var skills = await _volunteerService.GetAllSkillsAsync();
        return Ok(skills);
    }
}
