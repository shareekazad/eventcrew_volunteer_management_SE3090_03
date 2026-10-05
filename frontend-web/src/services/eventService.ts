import { apiClient } from './api';
import type { EventDto, CreateEventDto, EventStatus } from '../types/event';

// ============================================================================
// Venue types
// ============================================================================
export interface VenueDto {
  id: string;
  name: string;
  address: string;
  city: string;
  latitude: number | null;
  longitude: number | null;
  capacity: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateVenueDto {
  name: string;
  address: string;
  city: string;
  latitude: number | null;
  longitude: number | null;
  capacity: number;
}

// ============================================================================
// Event update DTO
// ============================================================================
export interface UpdateEventDto {
  venueId: string | null;
  title: string;
  description: string | null;
  category: string;
  startDate: string;
  endDate: string;
}

// ============================================================================
// Event service
// ============================================================================
export const eventService = {
  // ---- Events (read) ----

  /** Get all events. */
  async getAllEvents(): Promise<EventDto[]> {
    const response = await apiClient.get<EventDto[]>('/Events');
    return response.data;
  },

  /** Get a single event by ID. */
  async getEventById(id: string): Promise<EventDto> {
    const response = await apiClient.get<EventDto>(`/Events/${id}`);
    return response.data;
  },

  // ---- Events (write) ----

  /** Create a new event. */
  async createEvent(dto: CreateEventDto): Promise<EventDto> {
    const response = await apiClient.post<EventDto>('/Events', dto);
    return response.data;
  },

  /** Update an existing event. */
  async updateEvent(id: string, dto: UpdateEventDto): Promise<EventDto> {
    const response = await apiClient.put<EventDto>(`/Events/${id}`, dto);
    return response.data;
  },

  /** Transition an event to a new status. */
  async updateEventStatus(id: string, newStatus: EventStatus): Promise<EventDto> {
    const response = await apiClient.patch<EventDto>(`/Events/${id}/status`, {
      newStatus,
    });
    return response.data;
  },

  /** Delete an event. */
  async deleteEvent(id: string): Promise<void> {
    await apiClient.delete(`/Events/${id}`);
  },

  // ---- Venues ----

  /** Get all venues. */
  async getAllVenues(): Promise<VenueDto[]> {
    const response = await apiClient.get<VenueDto[]>('/Venues');
    return response.data;
  },

  /** Create a new venue. */
  async createVenue(dto: CreateVenueDto): Promise<VenueDto> {
    const response = await apiClient.post<VenueDto>('/Venues', dto);
    return response.data;
  },
};