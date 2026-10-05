import { apiClient } from './client'
import type { AttendanceActionRequest, AttendanceRecord, QrTokenResponse } from '../types/attendance'

export async function getShiftAttendance(shiftId: string): Promise<AttendanceRecord[]> {
  const response = await apiClient.get<AttendanceRecord[]>('/attendance', { params: { shiftId } })
  return response.data
}

export async function getAttendance(id: string): Promise<AttendanceRecord> {
  const response = await apiClient.get<AttendanceRecord>(`/attendance/${id}`)
  return response.data
}

export async function generateShiftQrToken(shiftId: string): Promise<QrTokenResponse> {
  const response = await apiClient.post<QrTokenResponse>(`/shifts/${shiftId}/qr-tokens`)
  return response.data
}

export async function checkInVolunteer(request: AttendanceActionRequest): Promise<AttendanceRecord> {
  const response = await apiClient.post<AttendanceRecord>('/attendance/check-in', request)
  return response.data
}

export async function checkOutVolunteer(request: AttendanceActionRequest): Promise<AttendanceRecord> {
  const response = await apiClient.post<AttendanceRecord>('/attendance/check-out', request)
  return response.data
}
