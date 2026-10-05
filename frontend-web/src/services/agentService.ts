import { apiClient } from './api';
import type {
  WorkflowRunStatusDto,
  WorkflowRunDetailDto,
} from '../types/agent';

/**
 * Service for the Agentic AI workflow.
 *
 * The reviewer ID (which user approves/rejects) is passed via the
 * X-Reviewer-Id header. This is temporary — once real JWT claims are
 * read server-side, the header disappears and the ID comes from the token.
 */
export const agentService = {
  /**
   * Trigger the PlanningAgent for an event.
   * Returns immediately with a run ID in AwaitingApproval status.
   */
  async planStaffing(eventId: string): Promise<WorkflowRunStatusDto> {
    const response = await apiClient.post<WorkflowRunStatusDto>(
      `/Agent/plan/${eventId}`
    );
    return response.data;
  },

  /** Get the current state of a workflow run (plan, status, review info). */
  async getRun(runId: string): Promise<WorkflowRunDetailDto> {
    const response = await apiClient.get<WorkflowRunDetailDto>(
      `/Agent/runs/${runId}`
    );
    return response.data;
  },

  /** Approve a workflow run. Requires reviewer ID. */
  async approveRun(
    runId: string,
    reviewerId: string
  ): Promise<WorkflowRunDetailDto> {
    const response = await apiClient.post<WorkflowRunDetailDto>(
      `/Agent/runs/${runId}/approve`,
      null,
      { headers: { 'X-Reviewer-Id': reviewerId } }
    );
    return response.data;
  },

  /** Reject a workflow run with a required reason. Requires reviewer ID. */
  async rejectRun(
    runId: string,
    reviewerId: string,
    reason: string
  ): Promise<WorkflowRunDetailDto> {
    const response = await apiClient.post<WorkflowRunDetailDto>(
      `/Agent/runs/${runId}/reject`,
      { reason },
      { headers: { 'X-Reviewer-Id': reviewerId } }
    );
    return response.data;
  },
};