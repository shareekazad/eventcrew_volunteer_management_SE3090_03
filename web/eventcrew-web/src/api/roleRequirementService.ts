import { apiClient } from './client'

export type RoleRequirementRecord = {
  id: string
  eventId: string
  roleName: string
  requiredHeadcount: number
}

export async function getRoleRequirements(eventId: string): Promise<RoleRequirementRecord[]> {
  const response = await apiClient.get<RoleRequirementRecord[]>('/role-requirements', { params: { eventId } })
  return response.data
}