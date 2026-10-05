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

// ============================================================
// Multi-agent workflow types (new)
// ============================================================

export interface EventSummary {
  id: string;
  organizer_id: string;
  venue_id: string | null;
  title: string;
  description: string | null;
  category: string;
  start_date: string;
  end_date: string;
  status: string;
  role_requirements: RoleRequirementSummary[];
}

export interface RoleRequirementSummary {
  id: string;
  role_name: string;
  required_headcount: number;
  min_experience_level: string;
}

export interface VenueSummary {
  id: string;
  name: string;
  address: string;
  city: string;
  capacity: number;
}

export interface StaffingRatioResult {
  venue_capacity: number;
  recommended_ushers: number;
  recommended_registration_staff: number;
  total_staff: number;
  reasoning: string;
}

export interface CandidateMatch {
  volunteer_id: string;
  volunteer_name: string;
  match_score: number;
  matching_skills: string[];
  experience_level: string;
  rating_score: number;
  justification: string;
}

export interface MatchingResult {
  role_name: string;
  status: string;
  matched_candidates: CandidateMatch[];
  unfulfilled_slots: number;
  execution_time_ms: number;
  error?: string;
}

export interface ProposedShift {
  shift_id: string;
  role_name: string;
  role_requirement_id: string | null;
  start_time: string;
  end_time: string;
  capacity: number;
  assigned_candidates: CandidateMatch[];
  reasoning: string;
}

export interface ShiftConflict {
  type: string;
  severity: 'low' | 'medium' | 'high';
  message: string;
  affected_ids: string[];
}

export interface AgentTrace {
  agent_name: string;
  tool_name: string;
  input_params: Record<string, unknown>;
  output_summary: Record<string, unknown>;
  duration_ms: number;
  called_at: string;
}

export interface WorkflowStateDto {
  event_id: string;
  workflow_id: string | null;
  current_step: number;

  // Planning
  event: EventSummary | null;
  venue: VenueSummary | null;
  staffing_ratio: StaffingRatioResult | null;
  plan_steps: PlanStepDto[];
  plan_reasoning: string | null;

  // Matching
  matching_results: MatchingResult[];
  total_matched: number;
  total_headcount_needed: number;

  // Scheduling
  proposed_shifts: ProposedShift[];
  shift_conflicts: ShiftConflict[];

  // Validation
  validation_passed: boolean;
  validation_errors: string[];
  validation_warnings: string[];

  // Audit + lifecycle
  agent_traces: AgentTrace[];
  status: string;
  error: string | null;
}