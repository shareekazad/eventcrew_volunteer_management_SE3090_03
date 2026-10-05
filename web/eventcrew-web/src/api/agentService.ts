import { apiClient } from './client'

export type WorkflowRunStatus = {
  runId: string
  eventId: string
  status: string
  objective?: string
  createdAt?: string
}

export type WorkflowRunDetail = {
  runId: string
  eventId: string
  status: string
  objective?: string
  planSummary?: string | null
  generatedRosterProposal?: string | null
  validationReport?: string | null
  reviewedByUserId?: string | null
  reviewNotes?: string | null
  createdAt?: string
  updatedAt?: string
  toolLogs?: Array<{
    logId: string
    agentName: string
    toolName: string
    inputParameters?: string | null
    outputSummary?: string | null
    executionDurationMs?: number
    calledAt?: string
  }>
}

export type PlannedRosterAssignment = {
  volunteer_id?: string
  volunteer_name?: string
  role?: string
  shift_id?: string
  shift_start?: string
  shift_end?: string
  hours?: number
}

export type ValidationSummary = {
  availability_violations?: number
  overlap_violations?: number
  hour_limit_violations?: number
  role_requirement_violations?: number
  total_assigned_volunteers?: number
  total_unfilled_slots?: number
  is_valid?: boolean
}

export async function planRoster(eventId: string): Promise<WorkflowRunStatus> {
  const response = await apiClient.post<WorkflowRunStatus>(`/Agent/plan/${eventId}`)
  return response.data
}

export async function getWorkflowRun(runId: string): Promise<WorkflowRunDetail> {
  const response = await apiClient.get<WorkflowRunDetail>(`/Agent/runs/${runId}`)
  return response.data
}

export async function approveRoster(runId: string): Promise<WorkflowRunDetail> {
  const response = await apiClient.post<WorkflowRunDetail>(`/Agent/runs/${runId}/approve`)
  return response.data
}

export async function rejectRoster(runId: string, reason: string): Promise<WorkflowRunDetail> {
  const response = await apiClient.post<WorkflowRunDetail>(`/Agent/runs/${runId}/reject`, { reason })
  return response.data
}
