// Type definitions for Events, mirroring the ASP.NET Core API DTOs.
// Keep these in sync with:
//   src/EventCrew.Api/DTOs/Events/EventResponseDto.cs
//   src/EventCrew.Api/DTOs/Events/CreateEventDto.cs
//   src/EventCrew.Api/DTOs/Events/RoleRequirementDto.cs

export type EventStatus =
  | 'Draft'
  | 'Published'
  | 'StaffingInProgress'
  | 'FullyStaffed'
  | 'Completed'
  | 'Cancelled';

export type ExperienceLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';

// ---- Role requirement ----

export interface RoleRequirementDto {
  id: string;
  roleName: string;
  description: string | null;
  requiredHeadcount: number;
  minExperienceLevel: ExperienceLevel;
  createdAt: string;
  updatedAt: string;
}

export interface RoleRequirementInputDto {
  roleName: string;
  description: string | null;
  requiredHeadcount: number;
  minExperienceLevel: ExperienceLevel;
}

// ---- Event ----

export interface EventDto {
  id: string;
  organizerId: string;
  venueId: string | null;
  title: string;
  description: string | null;
  category: string;
  startDate: string;   // ISO 8601 from API
  endDate: string;
  status: EventStatus;
  createdAt: string;
  updatedAt: string;
  roleRequirements: RoleRequirementDto[];
}

export interface CreateEventDto {
  organizerId: string;
  venueId: string | null;
  title: string;
  description: string | null;
  category: string;
  startDate: string;
  endDate: string;
  roleRequirements: RoleRequirementInputDto[];
}