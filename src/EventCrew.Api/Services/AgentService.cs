using System.Text;
using System.Text.Json;
using EventCrew.Api.DTOs.Agent;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

/// <summary>
/// Service that bridges ASP.NET Core to the Python AI service.
///
/// Responsibilities:
/// 1. Call the Python /agent/plan endpoint over HTTP.
/// 2. Persist the workflow run and tool-call audit trail to PostgreSQL.
/// 3. Return the structured plan.
/// </summary>
public class AgentService : IAgentService
{
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ILogger<AgentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AgentService(HttpClient http, AppDbContext db, ILogger<AgentService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    public async Task<PlanResultDto> PlanStaffingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        // ----------------------------------------------------------------
        // 1. Call the Python AI service
        // ----------------------------------------------------------------
        var requestBody = JsonSerializer.Serialize(
            new PlanRequestDto { EventId = eventId.ToString() });

        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling AI service for event {EventId}", eventId);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/agent/plan", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI service unreachable for event {EventId}", eventId);
            throw new InvalidOperationException("AI service is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "AI service timed out for event {EventId}", eventId);
            throw new InvalidOperationException("AI service timed out.", ex);
        }

        // Propagate business errors from Python (400 Bad Request)
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("AI service rejected plan for event {EventId}: {Detail}", eventId, detail);
            throw new InvalidOperationException($"AI service rejected the request: {detail}");
        }

        // Propagate service unavailable errors
        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("AI service unavailable for event {EventId}: {Detail}", eventId, detail);
            throw new InvalidOperationException($"AI service temporarily unavailable: {detail}");
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var plan = JsonSerializer.Deserialize<PlanResultDto>(json, JsonOptions)
                   ?? throw new InvalidOperationException("AI service returned an invalid plan.");

        _logger.LogInformation(
            "AI service returned plan for event {EventId}: {Steps} steps, {ToolCalls} tool calls",
            eventId, plan.Steps.Count, plan.ToolCalls.Count);

        // ----------------------------------------------------------------
        // 2. Persist the workflow run + tool logs
        // ----------------------------------------------------------------
        await PersistWorkflowRunAsync(eventId, plan, cancellationToken);

        return plan;
    }

    // ============================================================
    // Private helpers
    // ============================================================
    private async Task PersistWorkflowRunAsync(
        Guid eventId,
        PlanResultDto plan,
        CancellationToken cancellationToken)
    {
        // Find an organizer of this event to attribute the run to.
        // (Later this will come from the JWT-authenticated user.)
        var organizerId = await _db.Events
            .Where(e => e.Id == eventId)
            .Select(e => (Guid?)e.OrganizerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (organizerId is null)
        {
            _logger.LogWarning("Cannot persist workflow run: event {EventId} not found", eventId);
            return;
        }

        var planJson = JsonSerializer.Serialize(plan);

        var run = new AgentWorkflowRun
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            InitiatedByUserId = organizerId.Value,
            Status = "Running",
            PromptObjective = plan.Objective,
            PlanSummary = planJson,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Add tool logs via the navigation property so EF Core orders
        // the INSERT correctly (parent first, then children).
        foreach (var call in plan.ToolCalls)
        {
            run.ToolLogs.Add(new AgentToolLog
            {
                Id = Guid.NewGuid(),
                AgentName = "PlanningAgent",
                ToolName = call.ToolName,
                InputParameters = JsonSerializer.Serialize(call.InputParams),
                OutputSummary = JsonSerializer.Serialize(new { summary = call.OutputSummary }),
                ExecutionDurationMs = call.DurationMs,
                CalledAt = DateTimeOffset.UtcNow
            });
        }

        _db.AgentWorkflowRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Persisted workflow run {RunId} with {Count} tool log entries for event {EventId}",
            run.Id, run.ToolLogs.Count, eventId);
    }

    // ============================================================
    // Volunteer Matching Gateway — Section 9.1 & 10 (HITL)
    // ============================================================

    public async Task<MatchingResponseDto> MatchVolunteersAsync(
        MatchingRequestDto request,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Proxying volunteer matching request for event {EventId}, role '{Role}'",
            request.EventId, request.RoleName);

        // ------------------------------------------------------------------
        // 1. Fetch applicants from the DB — supply them to Python so it
        //    does NOT need to make a circular HTTP call back to this API.
        // ------------------------------------------------------------------
        var applications = await _db.Applications
            .Where(a => a.EventId == request.EventId)
            .Include(a => a.Volunteer)
                .ThenInclude(v => v.User)
            .Include(a => a.Volunteer)
                .ThenInclude(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)
            .ToListAsync(cancellationToken);

        var candidateList = applications.Select(a =>
        {
            var profile = a.Volunteer;
            var user = profile?.User;

            var skills = profile?.VolunteerSkills
                .Select(vs => vs.Skill?.Name ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList() ?? new List<string>();

            var highestProficiency = profile?.VolunteerSkills
                .Select(vs => vs.ProficiencyLevel)
                .OrderByDescending(p => p == "Advanced" ? 3 : p == "Intermediate" ? 2 : 1)
                .FirstOrDefault() ?? "Intermediate";

            var name = user?.FullName
                ?? a.VolunteerId.ToString();

            return new Dictionary<string, object?>
            {
                ["volunteer_id"] = a.VolunteerId.ToString(),
                ["volunteer_name"] = name,
                ["rating_score"] = (double)(profile?.RatingScore ?? 4.0m),
                ["skills"] = skills,
                ["experience_level"] = highestProficiency,
            };
        }).ToList();

        _logger.LogInformation(
            "Passing {Count} pre-fetched candidates to Python AI for event {EventId}",
            candidateList.Count, request.EventId.ToString());

        // ------------------------------------------------------------------
        // 2. Build the Python request payload (includes candidates to bypass
        //    the circular HTTP fetch inside the AI service).
        // ------------------------------------------------------------------
        var pythonPayload = new
        {
            event_id = request.EventId,
            role_name = request.RoleName,
            required_skills = request.RequiredSkills,
            min_experience_level = request.MinExperienceLevel,
            required_headcount = request.RequiredHeadcount,
            candidates = candidateList,
        };

        MatchingResponseDto matchingResult;

        // ------------------------------------------------------------------
        // 3. Try the Python AI microservice.
        // ------------------------------------------------------------------
        try
        {
            var body = JsonSerializer.Serialize(pythonPayload, JsonOptions);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                "/api/agents/match-volunteers", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                matchingResult = JsonSerializer.Deserialize<MatchingResponseDto>(json, JsonOptions)
                    ?? throw new InvalidOperationException("AI service returned null matching result.");
                matchingResult.IsFallback = false;
            }
            else
            {
                _logger.LogWarning(
                    "Python AI service returned {Status} for matching. Using C# fallback scorer.",
                    response.StatusCode);
                matchingResult = BuildFallbackMatchingResponse(request, candidateList);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Python AI service unreachable. Using C# fallback scorer.");
            matchingResult = BuildFallbackMatchingResponse(request, candidateList);
        }

        // ------------------------------------------------------------------
        // 4. Persist a workflow run record.
        // Status must be one of: 'Running', 'AwaitingApproval', 'Approved', 'Rejected', 'Failed'
        // ------------------------------------------------------------------
        var run = new AgentWorkflowRun
        {
            Id = matchingResult.WorkflowId == Guid.Empty ? Guid.NewGuid() : matchingResult.WorkflowId,
            EventId = request.EventId,
            InitiatedByUserId = initiatedByUserId,
            Status = "AwaitingApproval",
            PromptObjective = $"Match volunteers for role '{request.RoleName}' (headcount: {request.RequiredHeadcount})",
            GeneratedRosterProposal = JsonSerializer.Serialize(matchingResult, JsonOptions),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _db.AgentWorkflowRuns.Add(run);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            matchingResult.WorkflowRunId = run.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist matching workflow run (DB may be offline). Returning result without persistence.");
        }

        return matchingResult;
    }

    public async Task<MatchingResponseDto> ApproveMatchingAsync(
        ApproveMatchingRequestDto request,
        Guid reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Approving AI matching proposal for workflow run {RunId}", request.WorkflowRunId);

        try
        {
            var run = await _db.AgentWorkflowRuns
                .FirstOrDefaultAsync(r => r.Id == request.WorkflowRunId, cancellationToken);

            if (run is not null)
            {
                run.Status = "Approved";
                run.ReviewedByUserId = reviewerUserId;
                run.ReviewNotes = request.OrganizerNotes ?? "Approved by organizer";
                run.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist approval state (DB may be offline).");
        }

        return new MatchingResponseDto
        {
            WorkflowRunId = request.WorkflowRunId,
            Status = "SUCCESS",
            RoleName = "Approved",
            HeadcountNeeded = 0,
            UnfulfilledSlots = 0,
            ExecutionTimeMs = 0,
        };
    }

    public async Task<MatchingResponseDto> RejectMatchingAsync(
        RejectMatchingRequestDto request,
        Guid reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Rejecting AI matching proposal for workflow run {RunId}", request.WorkflowRunId);

        try
        {
            var run = await _db.AgentWorkflowRuns
                .FirstOrDefaultAsync(r => r.Id == request.WorkflowRunId, cancellationToken);

            if (run is not null)
            {
                run.Status = "Rejected";
                run.ReviewedByUserId = reviewerUserId;
                run.ReviewNotes = request.Reason;
                run.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist rejection state (DB may be offline).");
        }

        return new MatchingResponseDto
        {
            WorkflowRunId = request.WorkflowRunId,
            Status = "SAFE_FAILURE",
            RoleName = "Rejected",
            HeadcountNeeded = 0,
            UnfulfilledSlots = 0,
            ExecutionTimeMs = 0,
        };
    }

    /// <summary>
    /// Deterministic fallback scorer: runs Section 9.1 matching logic directly in C#
    /// when the Python AI service is unreachable or returns an error.
    /// Formula: 60% skills overlap + 30% rating + 10% experience tier.
    /// </summary>
    private static MatchingResponseDto BuildFallbackMatchingResponse(
        MatchingRequestDto request,
        List<Dictionary<string, object?>>? candidates = null)
    {
        var headcountNeeded = Math.Max(1, request.RequiredHeadcount);
        var requiredSkills = request.RequiredSkills ?? new List<string>();
        var minTier = request.MinExperienceLevel?.Trim().ToLowerInvariant() ?? "beginner";

        var scoredCandidates = new List<CandidateMatchDto>();

        if (candidates != null && candidates.Count > 0)
        {
            foreach (var c in candidates)
            {
                var volId = Guid.TryParse(c["volunteer_id"]?.ToString(), out var parsedId) ? parsedId : Guid.NewGuid();
                var name = c["volunteer_name"]?.ToString() ?? "Volunteer";
                var rating = c.TryGetValue("rating_score", out var rObj) && rObj is double rVal ? rVal : 4.5;
                var experience = c.TryGetValue("experience_level", out var eObj) ? eObj?.ToString() ?? "Intermediate" : "Intermediate";
                var volSkills = (c["skills"] as IEnumerable<string>)?.ToList()
                    ?? (c["skills"] as IEnumerable<object>)?.Select(s => s?.ToString() ?? "").ToList()
                    ?? new List<string>();

                var matchingSkills = requiredSkills.Count > 0
                    ? volSkills.Where(s => requiredSkills.Any(rs => string.Equals(rs, s, StringComparison.OrdinalIgnoreCase))).ToList()
                    : volSkills.Take(2).ToList();

                double skillOverlapRatio = requiredSkills.Count > 0
                    ? (double)matchingSkills.Count / requiredSkills.Count
                    : 1.0;

                // Guardrail 2: 0% skill match cannot be assigned to Advanced role
                if (minTier == "advanced" && requiredSkills.Count > 0 && matchingSkills.Count == 0)
                {
                    continue;
                }

                // Deterministic formula: 60% skills + 30% rating + 10% tier
                double skillsScore = skillOverlapRatio * 100.0;
                double ratingScore = (rating / 5.0) * 100.0;
                int tierValue = experience.Trim().ToLowerInvariant() switch
                {
                    "advanced" => 3,
                    "intermediate" => 2,
                    _ => 1
                };
                int minTierValue = minTier switch
                {
                    "advanced" => 3,
                    "intermediate" => 2,
                    _ => 1
                };
                double tierScore = tierValue >= minTierValue ? 100.0 : 50.0;

                double compositeScore = Math.Round((0.60 * skillsScore) + (0.30 * ratingScore) + (0.10 * tierScore), 1);

                string justification = requiredSkills.Count > 0
                    ? $"Qualified candidate with {(int)(skillOverlapRatio * 100)}% required skill overlap ({string.Join(", ", matchingSkills)}), past rating {rating:F1}/5.0, and {experience} experience tier for '{request.RoleName}'."
                    : $"Candidate assigned based on past rating {rating:F1}/5.0 and {experience} experience tier.";

                scoredCandidates.Add(new CandidateMatchDto
                {
                    VolunteerId = volId,
                    VolunteerName = name,
                    MatchScore = compositeScore,
                    MatchingSkills = matchingSkills,
                    ExperienceLevel = experience,
                    RatingScore = rating,
                    Justification = justification
                });
            }

            // Deterministic ranking: composite score descending, then rating score descending
            scoredCandidates = scoredCandidates
                .OrderByDescending(c => c.MatchScore)
                .ThenByDescending(c => c.RatingScore)
                .ToList();
        }

        var matched = scoredCandidates.Take(headcountNeeded).ToList();
        var status = matched.Count == 0
            ? "SAFE_FAILURE"
            : matched.Count < headcountNeeded
                ? "PARTIAL_MATCH"
                : "SUCCESS";

        return new MatchingResponseDto
        {
            WorkflowId = Guid.NewGuid(),
            RoleName = request.RoleName,
            HeadcountNeeded = headcountNeeded,
            MatchedCandidates = matched,
            UnfulfilledSlots = Math.Max(0, headcountNeeded - matched.Count),
            ExecutionTimeMs = 25,
            Status = status,
            IsFallback = true,
        };
    }
}