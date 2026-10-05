import { apiClient } from './client'
import type { AttendanceRecord, AttendanceStatistics, QrTokenResponse } from '../types/attendance'

type AttendanceFilters = { eventId: string; shiftId?: string }

export async function getAttendance(filters: AttendanceFilters): Promise<AttendanceRecord[]> {
  const response = await apiClient.get<AttendanceRecord[]>('/attendance', { params: filters })
  return response.data
}

export async function getAttendanceStatistics(filters: AttendanceFilters): Promise<AttendanceStatistics> {
  const response = await apiClient.get<AttendanceStatistics>('/attendance/statistics', { params: filters })
  return response.data
}

export async function getVolunteerAttendanceHistory(volunteerId: string): Promise<AttendanceRecord[]> {
  const response = await apiClient.get<AttendanceRecord[]>(`/attendance/volunteers/${volunteerId}/history`)
  return response.data
}

export async function generateAttendanceQrToken(
  shiftId: string,
  expiresAt: string,
): Promise<QrTokenResponse> {
  const response = await apiClient.post<QrTokenResponse>('/attendance/qr-tokens', { shiftId, expiresAt })
  return response.data
}
