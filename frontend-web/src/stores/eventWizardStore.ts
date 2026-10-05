import { create } from 'zustand';
import type { RoleRequirementInputDto, ExperienceLevel } from '../types/event';

// Shape of the wizard's internal state
interface WizardState {
  // ---- Step 1: Basic info ----
  title: string;
  description: string;
  category: string;
  startDate: string;   // ISO 8601
  endDate: string;

  // ---- Step 2: Venue ----
  venueId: string | null;

  // ---- Step 3: Role requirements ----
  roleRequirements: RoleRequirementInputDto[];

  // ---- Current step (0-indexed: 0..3) ----
  currentStep: number;

  // ---- Setters ----
  setBasicInfo: (info: {
    title: string;
    description: string;
    category: string;
    startDate: string;
    endDate: string;
  }) => void;
  setVenue: (venueId: string | null) => void;
  addRole: () => void;
  updateRole: (index: number, role: Partial<RoleRequirementInputDto>) => void;
  removeRole: (index: number) => void;

  // ---- Navigation ----
  nextStep: () => void;
  prevStep: () => void;
  goToStep: (step: number) => void;

  // ---- Utilities ----
  reset: () => void;
  isStepValid: (step: number) => boolean;
}

const TOTAL_STEPS = 4;

export const useEventWizardStore = create<WizardState>((set, get) => ({
  // ---- Initial state ----
  title: '',
  description: '',
  category: '',
  startDate: '',
  endDate: '',
  venueId: null,
  roleRequirements: [],
  currentStep: 0,

  // ---- Setters ----
  setBasicInfo: (info) => set(info),

  setVenue: (venueId) => set({ venueId }),

  addRole: () =>
    set((state) => ({
      roleRequirements: [
        ...state.roleRequirements,
        {
          roleName: '',
          description: null,
          requiredHeadcount: 1,
          minExperienceLevel: 'Beginner' as ExperienceLevel,
        },
      ],
    })),

  updateRole: (index, role) =>
    set((state) => {
      const updated = [...state.roleRequirements];
      updated[index] = { ...updated[index], ...role };
      return { roleRequirements: updated };
    }),

  removeRole: (index) =>
    set((state) => ({
      roleRequirements: state.roleRequirements.filter((_, i) => i !== index),
    })),

  // ---- Navigation ----
  nextStep: () =>
    set((state) => ({
      currentStep: Math.min(state.currentStep + 1, TOTAL_STEPS - 1),
    })),

  prevStep: () =>
    set((state) => ({
      currentStep: Math.max(state.currentStep - 1, 0),
    })),

  goToStep: (step) =>
    set({
      currentStep: Math.max(0, Math.min(step, TOTAL_STEPS - 1)),
    }),

  // ---- Utilities ----
  reset: () =>
    set({
      title: '',
      description: '',
      category: '',
      startDate: '',
      endDate: '',
      venueId: null,
      roleRequirements: [],
      currentStep: 0,
    }),

  isStepValid: (step) => {
    const s = get();
    switch (step) {
      case 0:
        return (
          s.title.trim() !== '' &&
          s.category.trim() !== '' &&
          s.startDate !== '' &&
          s.endDate !== '' &&
          new Date(s.startDate) < new Date(s.endDate)
        );
      case 1:
        return true; // venue is optional
      case 2:
        return s.roleRequirements.every(
          (r) => r.roleName.trim() !== '' && r.requiredHeadcount > 0
        );
      case 3:
        return true; // review step — validate everything on submit
      default:
        return false;
    }
  },
}));