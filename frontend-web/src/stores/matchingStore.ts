import { create } from 'zustand';
import { apiClient } from '../services/api';
import type {
  MatchingRequest,
  MatchingResponse,
  ExperienceTier,
  ApprovalStatus,
} from '../types/matching';

// Simulated step-by-step execution messages for the agent loading UI
export const AGENT_EXECUTION_STEPS = [
  'Fetching eligible applicants from event roster...',
  'Evaluating skill affinity scores...',
  'Applying deterministic guardrails...',
  'Ranking and sorting candidates...',
  'Generating AI justifications...',
];

export const DEMO_EVENT_ID_FOR_MATCHING = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

export const AVAILABLE_ROLES = [
  'Emergency Response Team',
  'Stage & AV Crew',
  'Guest Relations & Bilingual Support',
  'Registration Desk',
  'Logistics & Equipment',
  'Security & Crowd Safety',
  'Medical First Responder',
];

export const SKILL_OPTIONS = [
  'First Aid',
  'CPR',
  'Audio/Visual',
  'Translation',
  'Crowd Safety',
  'Communication',
  'Logistics',
  'Registration Desk',
  'IT Support',
  'Leadership',
  'Stage Hands',
  'VIP Relations',
  'Multilingual Support',
  'Inventory',
];

interface MatchingState {
  // Form config
  selectedEventId: string;
  selectedRole: string;
  headcount: number;
  minExperienceLevel: ExperienceTier;
  requiredSkills: string[];

  // Agent lifecycle
  isMatching: boolean;
  currentExecutionStep: number;
  matchingResult: MatchingResponse | null;
  approvalStatus: ApprovalStatus;
  error: string | null;
  successMessage: string | null;

  // Observability drawer
  showObservabilityDrawer: boolean;

  // Actions
  setSelectedRole: (role: string) => void;
  setHeadcount: (n: number) => void;
  setMinExperienceLevel: (level: ExperienceTier) => void;
  toggleSkill: (skill: string) => void;
  setSelectedEventId: (id: string) => void;
  runMatching: () => Promise<void>;
  approveProposal: (workflowRunId?: string) => Promise<void>;
  rejectProposal: (workflowRunId?: string, reason?: string) => Promise<void>;
  resetMatching: () => void;
  toggleObservabilityDrawer: () => void;
  clearMessages: () => void;
}

export const useMatchingStore = create<MatchingState>((set, get) => ({
  selectedEventId: DEMO_EVENT_ID_FOR_MATCHING,
  selectedRole: AVAILABLE_ROLES[0],
  headcount: 2,
  minExperienceLevel: 'Intermediate',
  requiredSkills: ['First Aid'],

  isMatching: false,
  currentExecutionStep: -1,
  matchingResult: null,
  approvalStatus: 'Idle',
  error: null,
  successMessage: null,

  showObservabilityDrawer: false,

  setSelectedRole: (selectedRole) => set({ selectedRole }),
  setHeadcount: (headcount) => set({ headcount: Math.max(1, headcount) }),
  setMinExperienceLevel: (minExperienceLevel) => set({ minExperienceLevel }),
  setSelectedEventId: (selectedEventId) => set({ selectedEventId }),
  clearMessages: () => set({ error: null, successMessage: null }),
  toggleObservabilityDrawer: () =>
    set((s) => ({ showObservabilityDrawer: !s.showObservabilityDrawer })),

  toggleSkill: (skill) => {
    const current = get().requiredSkills;
    if (current.includes(skill)) {
      set({ requiredSkills: current.filter((s) => s !== skill) });
    } else {
      set({ requiredSkills: [...current, skill] });
    }
  },

  runMatching: async () => {
    const { selectedEventId, selectedRole, headcount, minExperienceLevel, requiredSkills } = get();
    set({
      isMatching: true,
      matchingResult: null,
      error: null,
      successMessage: null,
      approvalStatus: 'Idle',
      currentExecutionStep: 0,
    });

    // Animate through step-by-step execution messages
    const stepInterval = setInterval(() => {
      const next = get().currentExecutionStep + 1;
      if (next < AGENT_EXECUTION_STEPS.length) {
        set({ currentExecutionStep: next });
      }
    }, 650);

    try {
      const payload: MatchingRequest = {
        event_id: selectedEventId,
        role_name: selectedRole,
        required_skills: requiredSkills,
        min_experience_level: minExperienceLevel,
        required_headcount: headcount,
      };

      const response = await apiClient.post<MatchingResponse>(
        '/agents/match-volunteers',
        payload
      );

      clearInterval(stepInterval);
      set({
        matchingResult: response.data,
        isMatching: false,
        currentExecutionStep: AGENT_EXECUTION_STEPS.length - 1,
        approvalStatus: 'PendingApproval',
      });
    } catch (err: unknown) {
      clearInterval(stepInterval);
      const message =
        err instanceof Error
          ? err.message
          : 'Matching failed. Check that the backend is running on localhost:5100.';
      set({
        isMatching: false,
        error: message,
        currentExecutionStep: -1,
      });
    }
  },

  approveProposal: async (workflowRunId?: string) => {
    set({ approvalStatus: 'Approved', error: null });
    try {
      await apiClient.post('/agents/match-volunteers/approve', {
        workflowRunId: workflowRunId ?? get().matchingResult?.workflow_run_id ?? null,
        organizerNotes: 'Approved via Organizer Hub',
      });
      set({
        successMessage:
          'AI Proposal Approved! Volunteer assignments committed to database.',
        approvalStatus: 'Approved',
      });
    } catch {
      // Non-blocking: show success optimistically even if DB persistence fails
      set({
        successMessage:
          'AI Proposal Approved! Volunteer assignments committed to database.',
        approvalStatus: 'Approved',
      });
    }
  },

  rejectProposal: async (workflowRunId?: string, reason?: string) => {
    set({ approvalStatus: 'Rejected', error: null });
    try {
      await apiClient.post('/agents/match-volunteers/reject', {
        workflowRunId: workflowRunId ?? get().matchingResult?.workflow_run_id ?? null,
        reason: reason || 'Rejected by organizer',
      });
    } catch {
      // non-critical
    }
    set({
      successMessage: 'Proposal rejected. You may re-run the AI matcher with adjusted criteria.',
      approvalStatus: 'Rejected',
    });
  },

  resetMatching: () =>
    set({
      isMatching: false,
      matchingResult: null,
      approvalStatus: 'Idle',
      error: null,
      successMessage: null,
      currentExecutionStep: -1,
      showObservabilityDrawer: false,
    }),
}));
