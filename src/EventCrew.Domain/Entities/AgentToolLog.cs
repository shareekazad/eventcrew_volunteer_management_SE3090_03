using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// An audit trail entry for a single tool call made during a workflow run.
/// Maps to the shared agent_tool_logs table.
/// </summary>
[Table("agent_tool_logs")]
public class AgentToolLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("workflow_run_id")]
    public Guid WorkflowRunId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("agent_name")]
    public string AgentName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("tool_name")]
    public string ToolName { get; set; } = string.Empty;

    [Column("input_parameters", TypeName = "jsonb")]
    public string? InputParameters { get; set; }

    [Column("output_summary", TypeName = "jsonb")]
    public string? OutputSummary { get; set; }

    [Required]
    [Column("execution_duration_ms")]
    public int ExecutionDurationMs { get; set; }

    [Column("called_at")]
    public DateTimeOffset CalledAt { get; set; } = DateTimeOffset.UtcNow;

    // ---- Navigation: tells EF Core this log belongs to a workflow run ----
    [ForeignKey(nameof(WorkflowRunId))]
    public AgentWorkflowRun? WorkflowRun { get; set; }
}