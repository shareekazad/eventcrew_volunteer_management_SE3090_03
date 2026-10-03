import { apiClient } from './client'
import type { CreateShiftSwapInput, ShiftSwapRequest } from '../types/shiftSwap'

export async function getAllShiftSwaps(): Promise<ShiftSwapRequest[]> {
  const response = await apiClient.get<ShiftSwapRequest[]>('/shift-swaps')
  return response.data
}

export async function getShiftSwapById(id: string): Promise<ShiftSwapRequest> {
  const response = await apiClient.get<ShiftSwapRequest>(`/shift-swaps/${id}`)
  return response.data
}

export async function createShiftSwap(input: CreateShiftSwapInput): Promise<ShiftSwapRequest> {
  const response = await apiClient.post<ShiftSwapRequest>('/shift-swaps', input)
  return response.data
}

export async function acceptShiftSwap(id: string): Promise<ShiftSwapRequest> {
  const response = await apiClient.post<ShiftSwapRequest>(`/shift-swaps/${id}/accept`)
  return response.data
}

export async function declineShiftSwap(id: string): Promise<ShiftSwapRequest> {
  const response = await apiClient.post<ShiftSwapRequest>(`/shift-swaps/${id}/reject`)
  return response.data
}

export async function cancelShiftSwap(id: string): Promise<ShiftSwapRequest> {
  const response = await apiClient.post<ShiftSwapRequest>(`/shift-swaps/${id}/cancel`)
  return response.data
}

export async function approveShiftSwap(id: string): Promise<ShiftSwapRequest> {
  const response = await apiClient.post<ShiftSwapRequest>(`/shift-swaps/${id}/approve`)
  return response.data
}

export async function rejectShiftSwapByOrganizer(id: string): Promise<ShiftSwapRequest> {
  const response = await apiClient.post<ShiftSwapRequest>(`/shift-swaps/${id}/organizer-reject`)
  return response.data
}
