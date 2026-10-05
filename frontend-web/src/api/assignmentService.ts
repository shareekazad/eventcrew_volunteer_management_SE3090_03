import { apiClient } from './client'
import type { CreateShiftAssignmentRequest, EligibleVolunteer, ShiftAssignment } from '../types/assignment'

type AssignmentFilters = { eventId?: string; shiftId?: string; volunteerId?: string }

export async function getAssignments(filters: AssignmentFilters = {}): Promise<ShiftAssignment[]> {
  const response = await apiClient.get<ShiftAssignment[]>('/assignments', { params: filters })
  return response.data
}

export async function getAssignment(id: string): Promise<ShiftAssignment> {
  const response = await apiClient.get<ShiftAssignment>(`/assignments/${id}`)
  return response.data
}

export async function getEligibleVolunteers(shiftId: string): Promise<EligibleVolunteer[]> {
  const response = await apiClient.get<EligibleVolunteer[]>('/assignments/eligible-volunteers', { params: { shiftId } })
  return response.data
}

export async function createAssignment(request: CreateShiftAssignmentRequest): Promise<ShiftAssignment> {
  const response = await apiClient.post<ShiftAssignment>('/assignments', request)
  return response.data
}

export async function deleteAssignment(id: string): Promise<void> {
  await apiClient.delete(`/assignments/${id}`)
}