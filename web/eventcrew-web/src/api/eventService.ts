import { apiClient } from './client'

export type EventRecord = { id: string; title: string }

export async function getAllEvents(): Promise<EventRecord[]> {
  const response = await apiClient.get<EventRecord[]>('/events')
  return response.data
}