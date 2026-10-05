import { apiClient } from './api';
import type { EventDto, CreateEventDto } from '../types/event';

// Type definitions for Venues (kept here since wizard needs them)
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

/**
 * Service for event and venue API calls.
 * Uses the shared axios client (which attaches the JWT automatically).
 */
export const eventService = {
  // ---- Events ----

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

  /** Create a new event. Returns the created event. */
  async createEvent(dto: CreateEventDto): Promise<EventDto> {
    const response = await apiClient.post<EventDto>('/Events', dto);
    return response.data;
  },

  /** Update an existing event. */
  async updateEvent(id: string, dto: Partial<CreateEventDto>): Promise<EventDto> {
    const response = await apiClient.put<EventDto>(`/Events/${id}`, dto);
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
};