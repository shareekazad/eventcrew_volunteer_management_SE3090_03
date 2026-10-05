// Type definitions for the AI Agent workflow, mirroring ASP.NET Core DTOs.
// Keep in sync with:
//   src/EventCrew.Api/DTOs/Agent/PlanResultDto.cs
//   src/EventCrew.Api/DTOs/Agent/WorkflowRunStatusDto.cs
//   src/EventCrew.Api/DTOs/Agent/WorkflowRunDetailDto.cs

export type WorkflowRunStatus =
  | 'Running'
  | 'AwaitingApproval'
  | 'Approved'
  | 'Rejected'
  | 'Failed';

// ---- Plan step ----
export interface PlanStepDto {
  step_number: number;
  action: string;
  tool: string | null;
  agent: string;
  status: string;
}

// ---- Tool call audit entry ----
export interface ToolCallDto {
  tool_name: string;
  input_params: Record<string, unknown>;
  output_summary: string;
  duration_ms: number;
  called_at: string;
}

// ---- Plan result (returned from POST /Agent/plan) ----
export interface PlanResultDto {
  objective: string;
  event_id: string;
  steps: PlanStepDto[];
  reasoning: string;
  tool_calls: ToolCallDto[];
  next_agent: string;
  status: string;
}

// ---- Run status (returned when plan is triggered) ----
export interface WorkflowRunStatusDto {
  runId: string;
  eventId: string;
  status: WorkflowRunStatus;
  objective: string;
  createdAt: string;
}

// ---- Full run details (returned from GET /Agent/runs/{id}) ----
export interface WorkflowRunDetailDto {
  runId: string;
  eventId: string;
  initiatedByUserId: string;
  status: WorkflowRunStatus;
  objective: string;
  planSummary: string | null;   // JSON string of PlanResultDto
  reviewedByUserId: string | null;
  reviewNotes: string | null;
  createdAt: string;
  updatedAt: string;
}