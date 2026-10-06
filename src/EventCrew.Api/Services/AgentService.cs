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
/// 1. Call the Python AI service — single-agent (/agent/plan) or
///    full 4-agent workflow (/workflow/plan).
/// 2. Persist workflow runs and every tool call as an audit trail.
/// 3. Manage human approval: approve / reject with audit info + email.
/// 4. Volunteer matching gateway for Student 2's matching agent.
/// </summary>
public class AgentService : IAgentService
{
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ILogger<AgentService> _logger;
    private readonly IEmailService _email;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AgentService(
        HttpClient http,
        AppDbContext db,
        ILogger<AgentService> logger,
        IEmailService email)
    {
        _http = http;
        _db = db;
        _logger = logger;
        _email = email;
    }

    // ============================================================
    // PLANNING WORKFLOW (Student 1) — runs the FULL 4-agent pipeline
    // ============================================================
    public async Task<WorkflowRunStatusDto> PlanStaffingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var requestBody = JsonSerializer.Serialize(
            new PlanRequestDto { EventId = eventId.ToString() });

        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling AI workflow for event {EventId}", eventId);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/workflow/plan", content, cancellationToken);
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

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("AI service rejected workflow for event {EventId}: {Detail}", eventId, detail);
            throw new InvalidOperationException($"AI service rejected the request: {detail}");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("AI service unavailable for event {EventId}: {Detail}", eventId, detail);
            throw new InvalidOperationException($"AI service temporarily unavailable: {detail}");
        }

        response.EnsureSuccessStatusCode();

        var workflowJson = await response.Content.ReadAsStringAsync(cancellationToken);

        string objective = "Multi-agent staffing plan";
        int traceCount = 0;
        try
        {
            using var doc = JsonDocument.Parse(workflowJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("plan_reasoning", out var reasonEl))
                objective = reasonEl.GetString() ?? objective;

            if (root.TryGetProperty("agent_traces", out var tracesEl))
                traceCount = tracesEl.GetArrayLength();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse workflow response JSON.");
        }

        _logger.LogInformation(
            "AI workflow returned for event {EventId}: {TraceCount} agent traces",
            eventId, traceCount);

        var run = await PersistWorkflowRunAsync(eventId, workflowJson, objective, cancellationToken);

        return new WorkflowRunStatusDto
        {
            RunId = run.Id,
            EventId = run.EventId,
            Status = run.Status,
            Objective = run.PromptObjective,
            CreatedAt = run.CreatedAt
        };
    }

    // ============================================================
    // GET — full run details
    // ============================================================
    public async Task<WorkflowRunDetailDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.AgentWorkflowRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        return run is null ? null : MapRunToDto(run);
    }

    // ============================================================
    // APPROVE — AwaitingApproval → Approved (with email)
    // ============================================================
    public async Task<WorkflowRunDetailDto?> ApproveAsync(Guid runId, Guid reviewedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await _db.AgentWorkflowRuns
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (run is null)
            return null;

        if (run.Status != "AwaitingApproval")
            throw new InvalidOperationException(
                $"Cannot approve a run in '{run.Status}' status. Only 'AwaitingApproval' runs can be approved.");

        run.Status = "Approved";
        run.ReviewedByUserId = reviewedByUserId;
        run.ReviewNotes = "Approved by organizer.";
        run.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Workflow run {RunId} approved by user {UserId}", runId, reviewedByUserId);

        // ---- Send approval email to the reviewer/organizer ----
        var organizerEmail = await _db.Users
            .Where(u => u.Id == reviewedByUserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        var eventTitle = await _db.Events
            .Where(e => e.Id == run.EventId)
            .Select(e => e.Title)
            .FirstOrDefaultAsync(cancellationToken) ?? "your event";

        if (!string.IsNullOrWhiteSpace(organizerEmail))
        {
            _ = _email.SendPlanApprovedAsync(
                organizerEmail,
                eventTitle,
                run.Id,
                CancellationToken.None);
        }

        return MapRunToDto(run);
    }

    // ============================================================
    // REJECT — AwaitingApproval → Rejected (with reason)
    // ============================================================
    public async Task<WorkflowRunDetailDto?> RejectAsync(Guid runId, Guid reviewedByUserId, string reason, CancellationToken cancellationToken = default)
    {
        var run = await _db.AgentWorkflowRuns
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (run is null)
            return null;

        if (run.Status != "AwaitingApproval")
            throw new InvalidOperationException(
                $"Cannot reject a run in '{run.Status}' status. Only 'AwaitingApproval' runs can be rejected.");

        run.Status = "Rejected";
        run.ReviewedByUserId = reviewedByUserId;
        run.ReviewNotes = reason;
        run.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Workflow run {RunId} rejected by user {UserId}: {Reason}", runId, reviewedByUserId, reason);

        return MapRunToDto(run);
    }

    // ============================================================
    // VOLUNTEER MATCHING WORKFLOW (Student 2)
    // ============================================================
    public async Task<MatchingResponseDto> MatchVolunteersAsync(
        MatchingRequestDto request,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Proxying volunteer matching request for event {EventId}, role '{Role}'",
            request.EventId, request.RoleName);

        // 1. Fetch applicants from DB and pre-load skills + user names
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

            var name = user?.FullName ?? a.VolunteerId.ToString();

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

        // 2. Build the Python payload
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

        // 3. Try the Python AI microservice
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

        // 4. Persist a workflow run record
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
            _logger.LogWarning(ex, "Could not persist matching workflow run (DB may be offline).");
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

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    private async Task<AgentWorkflowRun> PersistWorkflowRunAsync(
        Guid eventId,
        string workflowJson,
        string objective,
        CancellationToken cancellationToken)
    {
        var organizerId = await _db.Events
            .Where(e => e.Id == eventId)
            .Select(e => (Guid?)e.OrganizerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (organizerId is null)
            throw new InvalidOperationException($"Event '{eventId}' not found when persisting workflow run.");

        var traces = new List<JsonElement>();
        try
        {
            using var doc = JsonDocument.Parse(workflowJson);
            if (doc.RootElement.TryGetProperty("agent_traces", out var tracesEl))
            {
                foreach (var t in tracesEl.EnumerateArray())
                    traces.Add(t.Clone());
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse agent_traces from workflow JSON.");
        }

        var run = new AgentWorkflowRun
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            InitiatedByUserId = organizerId.Value,
            Status = "AwaitingApproval",
            PromptObjective = objective,
            PlanSummary = workflowJson,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        foreach (var trace in traces)
        {
            var agentName = trace.TryGetProperty("agent_name", out var a) ? a.GetString() : "Unknown";
            var toolName  = trace.TryGetProperty("tool_name", out var tn) ? tn.GetString() : "unknown";
            var duration  = trace.TryGetProperty("duration_ms", out var d) ? d.GetInt32() : 0;

            run.ToolLogs.Add(new AgentToolLog
            {
                Id = Guid.NewGuid(),
                AgentName = agentName ?? "Unknown",
                ToolName = toolName ?? "unknown",
                InputParameters = trace.TryGetProperty("input_params", out var ip)
                    ? ip.GetRawText() : "{}",
                OutputSummary = trace.TryGetProperty("output_summary", out var os)
                    ? os.GetRawText() : "{}",
                ExecutionDurationMs = duration,
                CalledAt = DateTimeOffset.UtcNow
            });
        }

        _db.AgentWorkflowRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Persisted workflow run {RunId} with {Count} tool logs for event {EventId}",
            run.Id, run.ToolLogs.Count, eventId);

        return run;
    }

    private static WorkflowRunDetailDto MapRunToDto(AgentWorkflowRun run) => new()
    {
        RunId = run.Id,
        EventId = run.EventId,
        InitiatedByUserId = run.InitiatedByUserId,
        Status = run.Status,
        Objective = run.PromptObjective,
        PlanSummary = run.PlanSummary,
        ReviewedByUserId = run.ReviewedByUserId,
        ReviewNotes = run.ReviewNotes,
        CreatedAt = run.CreatedAt,
        UpdatedAt = run.UpdatedAt
    };

    /// <summary>
    /// Deterministic fallback scorer (Section 9.1): 60% skills + 30% rating + 10% experience tier.
    /// Used when the Python AI service is unreachable or returns an error.
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
                var volId = Guid.TryParse(c["volunteer_id"]?.ToString(), out var parsedId)
                    ? parsedId : Guid.NewGuid();
                var name = c["volunteer_name"]?.ToString() ?? "Volunteer";
                var rating = c.TryGetValue("rating_score", out var rObj) && rObj is double rVal
                    ? rVal : 4.5;
                var experience = c.TryGetValue("experience_level", out var eObj)
                    ? eObj?.ToString() ?? "Intermediate" : "Intermediate";
                var volSkills = (c["skills"] as IEnumerable<string>)?.ToList()
                    ?? (c["skills"] as IEnumerable<object>)?.Select(s => s?.ToString() ?? "").ToList()
                    ?? new List<string>();

                var matchingSkills = requiredSkills.Count > 0
                    ? volSkills.Where(s => requiredSkills.Any(rs => string.Equals(rs, s, StringComparison.OrdinalIgnoreCase))).ToList()
                    : volSkills.Take(2).ToList();

                double skillOverlapRatio = requiredSkills.Count > 0
                    ? (double)matchingSkills.Count / requiredSkills.Count
                    : 1.0;

                // Guardrail: 0% skill match cannot be assigned to Advanced role
                if (minTier == "advanced" && requiredSkills.Count > 0 && matchingSkills.Count == 0)
                    continue;

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

                double compositeScore = Math.Round(
                    (0.60 * skillsScore) + (0.30 * ratingScore) + (0.10 * tierScore), 1);

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