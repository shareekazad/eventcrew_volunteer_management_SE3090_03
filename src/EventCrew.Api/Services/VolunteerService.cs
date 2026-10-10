using EventCrew.Api.DTOs;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

/// <summary>
/// Concrete implementation of <see cref="IVolunteerService"/>.
/// All business rules (duplicate application prevention, status-machine transitions)
/// are enforced here, keeping controllers thin.
/// </summary>
public class VolunteerService : IVolunteerService
{
    private readonly AppDbContext _db;

    // ── Valid status transitions (state-machine) ────────────────────────────
    private static readonly Dictionary<string, IReadOnlySet<string>> AllowedTransitions =
        new(StringComparer.Ordinal)
        {
            ["Submitted"]   = new HashSet<string> { "UnderReview" },
            ["UnderReview"] = new HashSet<string> { "Shortlisted", "Rejected" },
            ["Shortlisted"] = new HashSet<string> { "Accepted", "Rejected" },
            ["Accepted"]    = new HashSet<string>(),   // terminal
            ["Rejected"]    = new HashSet<string>(),   // terminal
        };

    public VolunteerService(AppDbContext db)
    {
        _db = db;
    }

    // ── Profile Upsert ──────────────────────────────────────────────────────

    public async Task<VolunteerProfileResponseDto> UpsertProfileAsync(
        Guid userId,
        CreateVolunteerProfileDto dto)
    {
        var now = DateTime.UtcNow;

        var profile = await _db.VolunteerProfiles
            .Include(vp => vp.VolunteerSkills)
                .ThenInclude(vs => vs.Skill)
            .FirstOrDefaultAsync(vp => vp.UserId == userId);

        if (profile is null)
        {
            // ── CREATE ──────────────────────────────────────────────────────
            profile = new VolunteerProfile
            {
                Id               = Guid.NewGuid(),
                UserId           = userId,
                EmergencyContact = dto.EmergencyContact,
                Bio              = dto.Bio,
                MaxHoursPerWeek  = dto.MaxHoursPerWeek,
                RatingScore      = 5.00m,
                CreatedAt        = now,
                UpdatedAt        = now,
            };

            _db.VolunteerProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }
        else
        {
            // ── UPDATE ──────────────────────────────────────────────────────
            profile.EmergencyContact = dto.EmergencyContact;
            profile.Bio              = dto.Bio;
            profile.MaxHoursPerWeek  = dto.MaxHoursPerWeek;
            profile.UpdatedAt        = now;

            // Remove stale skill links
            _db.VolunteerSkills.RemoveRange(profile.VolunteerSkills);
            await _db.SaveChangesAsync();
        }

        // ── Re-attach skills ────────────────────────────────────────────────
        if (dto.SkillIds.Any())
        {
            var newSkills = dto.SkillIds
                .Distinct()
                .Select(sid => new VolunteerSkill
                {
                    VolunteerId      = profile.Id,
                    SkillId          = sid,
                    ProficiencyLevel = "Intermediate",
                })
                .ToList();

            _db.VolunteerSkills.AddRange(newSkills);
            await _db.SaveChangesAsync();
        }

        // Reload with skills for response
        return await BuildProfileResponseAsync(profile.Id);
    }

    // ── Profile Retrieval ───────────────────────────────────────────────────

    public async Task<VolunteerProfileResponseDto?> GetProfileByUserIdAsync(Guid userId)
    {
        var profile = await _db.VolunteerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(vp => vp.UserId == userId);

        return profile is null ? null : await BuildProfileResponseAsync(profile.Id);
    }

    public async Task<VolunteerProfileResponseDto?> GetProfileByIdAsync(Guid profileId)
    {
        var exists = await _db.VolunteerProfiles
            .AsNoTracking()
            .AnyAsync(vp => vp.Id == profileId);

        return exists ? await BuildProfileResponseAsync(profileId) : null;
    }

    // ── Application Submission ──────────────────────────────────────────────

    public async Task<ApplicationResponseDto> ApplyForEventAsync(Guid userId, ApplyEventDto dto)
    {
        // The caller passes the USER id (from the JWT).
        // We must resolve the VolunteerProfile.id — a different row.
        var profile = await _db.VolunteerProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == userId);

        if (profile is null)
        {
            throw new InvalidOperationException(
                "No volunteer profile found for this user. Please create your profile before applying.");
        }

        var volunteerProfileId = profile.Id;

        // Business rule: one application per volunteer profile per event (409 Conflict guard)
        bool alreadyApplied = await _db.Applications
            .AnyAsync(a => a.EventId == dto.EventId && a.VolunteerId == volunteerProfileId);

        if (alreadyApplied)
        {
            throw new InvalidOperationException(
                $"You have already applied for this event.");
        }

        // Verify event exists
        var eventExists = await _db.Events.AnyAsync(e => e.Id == dto.EventId);
        if (!eventExists)
        {
            throw new InvalidOperationException("Event does not exist.");
        }

        var now = DateTime.UtcNow;
        var application = new Application
        {
            Id                = Guid.NewGuid(),
            EventId           = dto.EventId,
            VolunteerId       = volunteerProfileId,   // ← use the profile id, not the user id
            RoleRequirementId = dto.RoleRequirementId,
            Status            = "Submitted",
            Notes             = dto.Notes,
            AppliedAt         = now,
            ReviewedAt        = null,
            CreatedAt         = now,
            UpdatedAt         = now,
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        return MapApplicationToDto(application);
    }

    // ── Applicant Listing ───────────────────────────────────────────────────

    public async Task<IEnumerable<ApplicationResponseDto>> GetApplicantsByEventIdAsync(
        Guid eventId,
        string? statusFilter,
        int page = 1,
        int pageSize = 20)
    {
        IQueryable<Application> query = _db.Applications
            .AsNoTracking()
            .Where(a => a.EventId == eventId);

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(a => a.Status == statusFilter);

        var applications = await query
            .Include(a => a.Volunteer)
                .ThenInclude(v => v.User)
            .Include(a => a.Volunteer)
                .ThenInclude(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)
            .OrderByDescending(a => a.AppliedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return applications.Select(MapApplicationToDto);
    }

    // ── My Applications (volunteer self-service) ────────────────────────────

    public async Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByVolunteerIdAsync(Guid userId)
    {
        // Resolve the VolunteerProfile.id from the User.id
        var profile = await _db.VolunteerProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == userId);

        if (profile is null)
            return Enumerable.Empty<ApplicationResponseDto>();

        var applications = await _db.Applications
            .AsNoTracking()
            .Where(a => a.VolunteerId == profile.Id)
            .Include(a => a.Volunteer)
                .ThenInclude(v => v.User)
            .Include(a => a.Volunteer)
                .ThenInclude(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        return applications.Select(MapApplicationToDto);
    }

    // ── Status Update ───────────────────────────────────────────────────────

    public async Task<ApplicationResponseDto> UpdateApplicationStatusAsync(
        Guid applicationId,
        UpdateApplicationStatusDto dto)
    {
        var application = await _db.Applications.FindAsync(applicationId)
            ?? throw new KeyNotFoundException($"Application {applicationId} not found.");

        // Enforce state-machine transition rules
        if (!AllowedTransitions.TryGetValue(application.Status, out var allowedNext)
            || !allowedNext.Contains(dto.Status))
        {
            throw new InvalidOperationException(
                $"Cannot transition from '{application.Status}' to '{dto.Status}'. " +
                $"Allowed transitions from '{application.Status}': " +
                $"[{string.Join(", ", AllowedTransitions.GetValueOrDefault(application.Status, new HashSet<string>()))}].");
        }

        application.Status     = dto.Status;
        application.Notes      = dto.ReviewNotes ?? application.Notes;
        application.ReviewedAt = DateTime.UtcNow;
        application.UpdatedAt  = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapApplicationToDto(application);
    }

    // ── Private Helpers ─────────────────────────────────────────────────────

    private async Task<VolunteerProfileResponseDto> BuildProfileResponseAsync(Guid profileId)
    {
        var profile = await _db.VolunteerProfiles
            .AsNoTracking()
            .Include(vp => vp.User)
            .Include(vp => vp.VolunteerSkills)
                .ThenInclude(vs => vs.Skill)
            .FirstAsync(vp => vp.Id == profileId);

        return new VolunteerProfileResponseDto
        {
            Id               = profile.Id,
            UserId           = profile.UserId,
            FullName         = profile.User?.FullName,
            Email            = profile.User?.Email,
            EmergencyContact = profile.EmergencyContact,
            Bio              = profile.Bio,
            MaxHoursPerWeek  = profile.MaxHoursPerWeek,
            RatingScore      = profile.RatingScore,
            Skills           = profile.VolunteerSkills.Select(vs => new SkillDto
            {
                Id               = vs.Skill.Id,
                Name             = vs.Skill.Name,
                Category         = vs.Skill.Category,
                ProficiencyLevel = vs.ProficiencyLevel,
            }).ToList(),
        };
    }

    private static ApplicationResponseDto MapApplicationToDto(Application app) =>
        new()
        {
            Id                = app.Id,
            EventId           = app.EventId,
            VolunteerId       = app.VolunteerId,
            RoleRequirementId = app.RoleRequirementId,
            Status            = app.Status,
            Notes             = app.Notes,
            AppliedAt         = app.AppliedAt,
            ReviewedAt        = app.ReviewedAt,
            Volunteer         = app.Volunteer is null ? null : new VolunteerProfileResponseDto
            {
                Id               = app.Volunteer.Id,
                UserId           = app.Volunteer.UserId,
                FullName         = app.Volunteer.User?.FullName,
                Email            = app.Volunteer.User?.Email,
                EmergencyContact = app.Volunteer.EmergencyContact,
                Bio              = app.Volunteer.Bio,
                MaxHoursPerWeek  = app.Volunteer.MaxHoursPerWeek,
                RatingScore      = app.Volunteer.RatingScore,
                Skills           = app.Volunteer.VolunteerSkills.Select(vs => new SkillDto
                {
                    Id               = vs.Skill.Id,
                    Name             = vs.Skill.Name,
                    Category         = vs.Skill.Category,
                    ProficiencyLevel = vs.ProficiencyLevel,
                }).ToList(),
            }
        };
}