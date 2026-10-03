// Types mirroring the Python MatchingSchema & ASP.NET Core MatchingResponseDto

export type ExperienceTier = 'Beginner' | 'Intermediate' | 'Advanced';
export type MatchingStatus = 'SUCCESS' | 'PARTIAL_MATCH' | 'SAFE_FAILURE';
export type ApprovalStatus = 'Idle' | 'PendingApproval' | 'Approved' | 'Rejected';

export interface MatchingRequest {
  event_id: string;
  role_name: string;
  required_skills: string[];
  min_experience_level: ExperienceTier;
  required_headcount: number;
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

export interface MatchingResponse {
  workflow_id: string;
  role_name: string;
  headcount_needed: number;
  matched_candidates: CandidateMatch[];
  unfulfilled_slots: number;
  execution_time_ms: number;
  status: MatchingStatus;
  workflow_run_id?: string;
  is_fallback?: boolean;
}
