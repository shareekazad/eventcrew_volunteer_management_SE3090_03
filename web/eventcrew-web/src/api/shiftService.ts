import { apiClient } from './client'
import type { Shift, ShiftWriteRequest } from '../types/shift'

const shiftsPath = '/shifts'

export async function getAllShifts(): Promise<Shift[]> {
  const response = await apiClient.get<Shift[]>(shiftsPath)
  return response.data
}

export async function getShift(id: string): Promise<Shift> {
  const response = await apiClient.get<Shift>(`${shiftsPath}/${id}`)
  return response.data
}

export async function createShift(request: ShiftWriteRequest): Promise<Shift> {
  const response = await apiClient.post<Shift>(shiftsPath, request)
  return response.data
}

export async function updateShift(id: string, request: ShiftWriteRequest): Promise<void> {
  await apiClient.put(`${shiftsPath}/${id}`, request)
}

export async function deleteShift(id: string): Promise<void> {
  await apiClient.delete(`${shiftsPath}/${id}`)
}