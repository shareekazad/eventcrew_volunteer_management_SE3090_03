export type ApplicationStatus =
  | 'Submitted'
  | 'UnderReview'
  | 'Shortlisted'
  | 'Accepted'
  | 'Rejected';

export type StatusFilter = 'All' | ApplicationStatus;

export interface Skill {
  id: string;
  name: string;
  category: string;
  proficiencyLevel: string;
}

export interface VolunteerProfile {
  id: string;
  userId: string;
  fullName?: string;
  email?: string;
  emergencyContact: string;
  bio?: string | null;
  maxHoursPerWeek: number;
  ratingScore: number;
  skills: Skill[];
  experienceYears?: number;
  pastEventsCount?: number;
}

export interface Application {
  id: string;
  eventId: string;
  volunteerId: string;
  roleRequirementId?: string | null;
  status: ApplicationStatus;
  notes?: string | null;
  appliedAt: string;
  reviewedAt?: string | null;
  volunteer?: VolunteerProfile;
}

export interface UpdateApplicationStatusPayload {
  status: ApplicationStatus;
  reviewNotes?: string;
}
