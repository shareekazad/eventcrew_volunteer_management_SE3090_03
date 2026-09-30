using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// Represents one execution of the AI workflow for an event.
/// Persists the objective, plan, and lifecycle of the run.
/// Maps to the shared agent_workflow_runs table.
/// </summary>
[Table("agent_workflow_runs")]
public class AgentWorkflowRun
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("event_id")]
    public Guid EventId { get; set; }

    [Required]
    [Column("initiated_by_user_id")]
    public Guid InitiatedByUserId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "Running";

    [Required]
    [Column("prompt_objective")]
    public string PromptObjective { get; set; } = string.Empty;

    [Column("plan_summary", TypeName = "jsonb")]
    public string? PlanSummary { get; set; }

    [Column("generated_roster_proposal", TypeName = "jsonb")]
    public string? GeneratedRosterProposal { get; set; }

    [Column("validation_report", TypeName = "jsonb")]
    public string? ValidationReport { get; set; }

    [Column("reviewed_by_user_id")]
    public Guid? ReviewedByUserId { get; set; }

    [Column("review_notes")]
    public string? ReviewNotes { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ---- Navigation: many tool logs belong to one run ----
    public ICollection<AgentToolLog> ToolLogs { get; set; } = new List<AgentToolLog>();
}