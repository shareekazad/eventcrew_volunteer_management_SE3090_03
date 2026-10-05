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
/// 4. Return structured DTOs to the controller.
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
    // VOLUNTEER MATCHING WORKFLOW (Student 2) — direct proxy
    // ============================================================
    public async Task<MatchingResponseDto> MatchVolunteersAsync(
        MatchingRequestDto request,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Proxying volunteer matching request for event {EventId}, role '{Role}'",
            request.EventId, request.RoleName);

        MatchingResponseDto matchingResult;

        try
        {
            var body = JsonSerializer.Serialize(request, JsonOptions);
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
                    "Python AI service returned {Status} for matching. Using fallback.",
                    response.StatusCode);
                matchingResult = BuildFallbackMatchingResponse(request);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Python AI service unreachable. Using graceful fallback for matching.");
            matchingResult = BuildFallbackMatchingResponse(request);
        }

        var run = new AgentWorkflowRun
        {
            Id = matchingResult.WorkflowId == Guid.Empty ? Guid.NewGuid() : matchingResult.WorkflowId,
            EventId = request.EventId,
            InitiatedByUserId = initiatedByUserId,
            Status = "Completed",
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

    private static MatchingResponseDto BuildFallbackMatchingResponse(MatchingRequestDto request)
    {
        var skillsRequired = request.RequiredSkills.Count > 0
            ? request.RequiredSkills
            : new List<string> { "First Aid", "Communication" };

        var candidates = new List<CandidateMatchDto>
        {
            new()
            {
                VolunteerId = Guid.Parse("c1000000-0000-0000-0000-000000000001"),
                VolunteerName = "Sarah Jenkins",
                MatchScore = 96.5,
                MatchingSkills = skillsRequired.Take(Math.Min(skillsRequired.Count, 2)).ToList(),
                ExperienceLevel = request.MinExperienceLevel == "Advanced" ? "Advanced" : "Intermediate",
                RatingScore = 4.95,
                Justification = "Strong candidate with high skill overlap, rating 4.95/5.0.",
            },
            new()
            {
                VolunteerId = Guid.Parse("c1000000-0000-0000-0000-000000000002"),
                VolunteerName = "David Chen",
                MatchScore = 82.0,
                MatchingSkills = skillsRequired.Take(Math.Min(skillsRequired.Count, 1)).ToList(),
                ExperienceLevel = "Intermediate",
                RatingScore = 4.80,
                Justification = "Qualified candidate with partial skill overlap.",
            },
            new()
            {
                VolunteerId = Guid.Parse("c1000000-0000-0000-0000-000000000003"),
                VolunteerName = "Elena Rostova",
                MatchScore = 74.5,
                MatchingSkills = skillsRequired.Take(1).ToList(),
                ExperienceLevel = "Intermediate",
                RatingScore = 4.70,
                Justification = "Good candidate with communication background.",
            },
        };

        var headcountNeeded = Math.Max(1, request.RequiredHeadcount);
        var matched = candidates.Take(headcountNeeded).ToList();
        var status = matched.Count >= headcountNeeded ? "SUCCESS"
            : matched.Count > 0 ? "PARTIAL_MATCH"
            : "SAFE_FAILURE";

        return new MatchingResponseDto
        {
            WorkflowId = Guid.NewGuid(),
            RoleName = request.RoleName,
            HeadcountNeeded = headcountNeeded,
            MatchedCandidates = matched,
            UnfulfilledSlots = Math.Max(0, headcountNeeded - matched.Count),
            ExecutionTimeMs = 38,
            Status = status,
            IsFallback = true,
        };
    }
}