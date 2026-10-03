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
/// 2. Persist the workflow run (status = AwaitingApproval) and tool-call audit trail.
/// 3. Manage human approval: approve / reject with audit info.
/// 4. Return structured DTOs to the controller.
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

    // ============================================================
    // PLAN — runs workflow, leaves run in AwaitingApproval
    // ============================================================
    public async Task<WorkflowRunStatusDto> PlanStaffingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        // ------------------------------------------------------------
        // 1. Call the Python AI service
        // ------------------------------------------------------------
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

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("AI service rejected plan for event {EventId}: {Detail}", eventId, detail);
            throw new InvalidOperationException($"AI service rejected the request: {detail}");
        }

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

        // ------------------------------------------------------------
        // 2. Persist the workflow run + tool logs, status = AwaitingApproval
        // ------------------------------------------------------------
        var run = await PersistWorkflowRunAsync(eventId, plan, cancellationToken);

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
    // APPROVE — AwaitingApproval → Approved
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
    // PRIVATE HELPERS
    // ============================================================
    private async Task<AgentWorkflowRun> PersistWorkflowRunAsync(
        Guid eventId,
        PlanResultDto plan,
        CancellationToken cancellationToken)
    {
        var organizerId = await _db.Events
            .Where(e => e.Id == eventId)
            .Select(e => (Guid?)e.OrganizerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (organizerId is null)
            throw new InvalidOperationException($"Event '{eventId}' not found when persisting workflow run.");

        var planJson = JsonSerializer.Serialize(plan);

        var run = new AgentWorkflowRun
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            InitiatedByUserId = organizerId.Value,
            Status = "AwaitingApproval",     // ← paused for human approval
            PromptObjective = plan.Objective,
            PlanSummary = planJson,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

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
}