import { create } from 'zustand';
import { apiClient } from '../services/api';
import type { Application, ApplicationStatus, StatusFilter, VolunteerProfile } from '../types/application';

// Realistic fallback profiles for demo/enrichment when volunteer data is sparse
const DEMO_NAMES = [
  { name: 'Sarah Jenkins', bio: 'Experienced medical lead and certified first responder for outdoor festivals.', contact: '+1 (555) 234-5678', rating: 4.95, exp: 4, skills: [{ id: 's1', name: 'First Aid', category: 'Medical', proficiencyLevel: 'Expert' }, { id: 's2', name: 'Crowd Safety', category: 'Operations', proficiencyLevel: 'Intermediate' }] },
  { name: 'David Chen', bio: 'AV technician and stage management volunteer with 5+ major conventions.', contact: '+1 (555) 876-5432', rating: 4.80, exp: 3, skills: [{ id: 's3', name: 'Audio/Visual', category: 'Technical', proficiencyLevel: 'Expert' }, { id: 's4', name: 'Stage Hands', category: 'Logistics', proficiencyLevel: 'Intermediate' }] },
  { name: 'Elena Rostova', bio: 'Guest relations specialist fluent in 3 languages. Passionate about community engagement.', contact: '+1 (555) 432-1098', rating: 4.90, exp: 5, skills: [{ id: 's5', name: 'Multilingual Support', category: 'Hospitality', proficiencyLevel: 'Expert' }, { id: 's6', name: 'VIP Relations', category: 'Hospitality', proficiencyLevel: 'Advanced' }] },
  { name: 'Marcus Johnson', bio: 'Logistics coordinator with heavy-lifting and equipment setup background.', contact: '+1 (555) 321-7654', rating: 4.65, exp: 2, skills: [{ id: 's7', name: 'Logistics', category: 'Operations', proficiencyLevel: 'Advanced' }, { id: 's8', name: 'Inventory', category: 'Logistics', proficiencyLevel: 'Intermediate' }] },
  { name: 'Amina Patel', bio: 'Tech enthusiast helping with registration desk, badge printing, and attendee app onboarding.', contact: '+1 (555) 987-6543', rating: 4.88, exp: 3, skills: [{ id: 's9', name: 'Registration Desk', category: 'Administration', proficiencyLevel: 'Expert' }, { id: 's10', name: 'IT Support', category: 'Technical', proficiencyLevel: 'Intermediate' }] },
  { name: 'Lucas Rivera', bio: 'Safety and security team member with experience managing stadium entry gates.', contact: '+1 (555) 654-9870', rating: 4.70, exp: 2, skills: [{ id: 's11', name: 'Crowd Safety', category: 'Operations', proficiencyLevel: 'Advanced' }] },
];

export const DEMO_EVENT_ID = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

export const INITIAL_DEMO_APPLICANTS: Application[] = [
  {
    id: 'b1000000-0000-0000-0000-000000000001',
    eventId: DEMO_EVENT_ID,
    volunteerId: 'c1000000-0000-0000-0000-000000000001',
    status: 'Submitted',
    notes: 'Available for both setup and breakdown shifts.',
    appliedAt: new Date(Date.now() - 3600000 * 4).toISOString(),
    volunteer: {
      id: 'c1000000-0000-0000-0000-000000000001',
      userId: 'u1',
      fullName: DEMO_NAMES[0].name,
      email: 'sarah.jenkins@example.com',
      emergencyContact: DEMO_NAMES[0].contact,
      bio: DEMO_NAMES[0].bio,
      maxHoursPerWeek: 20,
      ratingScore: DEMO_NAMES[0].rating,
      experienceYears: DEMO_NAMES[0].exp,
      pastEventsCount: 8,
      skills: DEMO_NAMES[0].skills,
    },
  },
  {
    id: 'b1000000-0000-0000-0000-000000000002',
    eventId: DEMO_EVENT_ID,
    volunteerId: 'c1000000-0000-0000-0000-000000000002',
    status: 'UnderReview',
    notes: 'Can bring own sound monitoring gear if needed.',
    appliedAt: new Date(Date.now() - 3600000 * 18).toISOString(),
    volunteer: {
      id: 'c1000000-0000-0000-0000-000000000002',
      userId: 'u2',
      fullName: DEMO_NAMES[1].name,
      email: 'david.chen@example.com',
      emergencyContact: DEMO_NAMES[1].contact,
      bio: DEMO_NAMES[1].bio,
      maxHoursPerWeek: 15,
      ratingScore: DEMO_NAMES[1].rating,
      experienceYears: DEMO_NAMES[1].exp,
      pastEventsCount: 6,
      skills: DEMO_NAMES[1].skills,
    },
  },
  {
    id: 'b1000000-0000-0000-0000-000000000003',
    eventId: DEMO_EVENT_ID,
    volunteerId: 'c1000000-0000-0000-0000-000000000003',
    status: 'Shortlisted',
    notes: 'Experienced bilingual host.',
    appliedAt: new Date(Date.now() - 3600000 * 32).toISOString(),
    volunteer: {
      id: 'c1000000-0000-0000-0000-000000000003',
      userId: 'u3',
      fullName: DEMO_NAMES[2].name,
      email: 'elena.rostova@example.com',
      emergencyContact: DEMO_NAMES[2].contact,
      bio: DEMO_NAMES[2].bio,
      maxHoursPerWeek: 25,
      ratingScore: DEMO_NAMES[2].rating,
      experienceYears: DEMO_NAMES[2].exp,
      pastEventsCount: 12,
      skills: DEMO_NAMES[2].skills,
    },
  },
  {
    id: 'b1000000-0000-0000-0000-000000000004',
    eventId: DEMO_EVENT_ID,
    volunteerId: 'c1000000-0000-0000-0000-000000000004',
    status: 'Accepted',
    notes: 'Approved for main stage gear crew.',
    appliedAt: new Date(Date.now() - 3600000 * 50).toISOString(),
    reviewedAt: new Date(Date.now() - 3600000 * 12).toISOString(),
    volunteer: {
      id: 'c1000000-0000-0000-0000-000000000004',
      userId: 'u4',
      fullName: DEMO_NAMES[3].name,
      email: 'marcus.j@example.com',
      emergencyContact: DEMO_NAMES[3].contact,
      bio: DEMO_NAMES[3].bio,
      maxHoursPerWeek: 30,
      ratingScore: DEMO_NAMES[3].rating,
      experienceYears: DEMO_NAMES[3].exp,
      pastEventsCount: 4,
      skills: DEMO_NAMES[3].skills,
    },
  },
  {
    id: 'b1000000-0000-0000-0000-000000000005',
    eventId: DEMO_EVENT_ID,
    volunteerId: 'c1000000-0000-0000-0000-000000000005',
    status: 'Rejected',
    notes: 'Schedule conflict with university exams.',
    appliedAt: new Date(Date.now() - 3600000 * 70).toISOString(),
    reviewedAt: new Date(Date.now() - 3600000 * 20).toISOString(),
    volunteer: {
      id: 'c1000000-0000-0000-0000-000000000005',
      userId: 'u5',
      fullName: DEMO_NAMES[4].name,
      email: 'amina.patel@example.com',
      emergencyContact: DEMO_NAMES[4].contact,
      bio: DEMO_NAMES[4].bio,
      maxHoursPerWeek: 10,
      ratingScore: DEMO_NAMES[4].rating,
      experienceYears: DEMO_NAMES[4].exp,
      pastEventsCount: 3,
      skills: DEMO_NAMES[4].skills,
    },
  },
];

interface ApplicationState {
  currentEventId: string;
  applicants: Application[];
  selectedApplicant: Application | null;
  isLoading: boolean;
  error: string | null;
  successMessage: string | null;
  searchQuery: string;
  statusFilter: StatusFilter;
  sortBy: 'date-desc' | 'date-asc' | 'rating-desc' | 'name-asc';

  // Actions
  setCurrentEventId: (eventId: string) => void;
  fetchApplicants: (eventId?: string) => Promise<void>;
  updateStatus: (applicationId: string, status: ApplicationStatus, notes?: string) => Promise<boolean>;
  setSearchQuery: (query: string) => void;
  setStatusFilter: (filter: StatusFilter) => void;
  setSortBy: (sortBy: ApplicationState['sortBy']) => void;
  setSelectedApplicant: (applicant: Application | null) => void;
  clearMessages: () => void;
  loadDemoData: () => void;
}

export const useApplicationStore = create<ApplicationState>((set, get) => ({
  currentEventId: DEMO_EVENT_ID,
  applicants: INITIAL_DEMO_APPLICANTS,
  selectedApplicant: null,
  isLoading: false,
  error: null,
  successMessage: null,
  searchQuery: '',
  statusFilter: 'All',
  sortBy: 'date-desc',

  setCurrentEventId: (eventId: string) => {
    set({ currentEventId: eventId });
  },

  setSearchQuery: (searchQuery: string) => set({ searchQuery }),

  setStatusFilter: (statusFilter: StatusFilter) => set({ statusFilter }),

  setSortBy: (sortBy) => set({ sortBy }),

  setSelectedApplicant: (selectedApplicant: Application | null) => set({ selectedApplicant }),

  clearMessages: () => set({ error: null, successMessage: null }),

  loadDemoData: () => {
    set({
      applicants: INITIAL_DEMO_APPLICANTS,
      error: null,
      successMessage: 'Loaded demo applicants successfully.',
    });
  },

  fetchApplicants: async (eventId?: string) => {
    const targetEventId = eventId || get().currentEventId;
    set({ isLoading: true, error: null, currentEventId: targetEventId });

    try {
      // Backend endpoint: GET /api/applications/event/{eventId}
      const response = await apiClient.get<Application[]>(`/applications/event/${targetEventId}`);
      const rawApplications = response.data || [];

      // Hydrate volunteer details from /api/volunteers/{id} or fallback metadata
      const enrichedApplicants = await Promise.all(
        rawApplications.map(async (app, index) => {
          let profile: VolunteerProfile | undefined;
          try {
            const profRes = await apiClient.get<VolunteerProfile>(`/volunteers/${app.volunteerId}`);
            profile = profRes.data;
          } catch {
            // Graceful fallback for mock/unseeded volunteer ID
            const fallback = DEMO_NAMES[index % DEMO_NAMES.length];
            profile = {
              id: app.volunteerId,
              userId: `user-${app.volunteerId.slice(0, 8)}`,
              fullName: fallback.name,
              email: `${fallback.name.toLowerCase().replace(/\s+/g, '.')}@example.com`,
              emergencyContact: fallback.contact,
              bio: fallback.bio,
              maxHoursPerWeek: 15 + (index * 5) % 15,
              ratingScore: fallback.rating,
              experienceYears: fallback.exp,
              pastEventsCount: 3 + index,
              skills: fallback.skills,
            };
          }

          return {
            ...app,
            volunteer: profile,
          };
        })
      );

      // If backend has 0 applicants for this new ID, keep empty so user can see empty state
      set({
        applicants: enrichedApplicants,
        isLoading: false,
        error: null,
      });
    } catch (err: any) {
      console.warn('Backend fetch failed, offering demo data or showing error:', err);
      // If error occurs (e.g. backend offline or not seeded), set error message
      set({
        isLoading: false,
        error: err.message || 'Unable to connect to http://localhost:5100/api. Check that the backend is running.',
      });
    }
  },

  updateStatus: async (applicationId: string, newStatus: ApplicationStatus, notes?: string) => {
    set({ isLoading: true, error: null, clearMessages: get().clearMessages });

    try {
      // Backend endpoint: PUT /api/applications/{id}/status
      await apiClient.put(`/applications/${applicationId}/status`, {
        status: newStatus,
        reviewNotes: notes || undefined,
      });

      // Update state locally
      const updatedList = get().applicants.map((app) => {
        if (app.id === applicationId) {
          const updated: Application = {
            ...app,
            status: newStatus,
            notes: notes !== undefined ? notes : app.notes,
            reviewedAt: new Date().toISOString(),
          };
          // Also sync selected applicant if open in modal
          if (get().selectedApplicant?.id === applicationId) {
            set({ selectedApplicant: updated });
          }
          return updated;
        }
        return app;
      });

      set({
        applicants: updatedList,
        isLoading: false,
        successMessage: `Volunteer status updated to "${newStatus}" successfully!`,
      });
      return true;
    } catch (err: any) {
      const errorMsg = err.message || 'Failed to update application status.';
      set({
        isLoading: false,
        error: errorMsg,
      });
      return false;
    }
  },
}));
