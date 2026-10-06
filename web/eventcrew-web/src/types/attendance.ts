export type AttendanceStatus = 'Pending' | 'CheckedIn' | 'CheckedOut' | 'Absent' | 'Excused'

export type AttendanceRecord = {
  id: string | null
  volunteerId: string
  eventId: string
  eventTitle: string
  shiftId: string
  shiftTitle: string
  checkInTime: string | null
  checkOutTime: string | null
  status: AttendanceStatus
  verifiedHours: number
  createdAt: string | null
  updatedAt: string | null
}

export type AttendanceStatistics = {
  eventId: string
  shiftId: string | null
  total: number
  pending: number
  checkedIn: number
  checkedOut: number
  absent: number
  excused: number
  verifiedHours: number
}

export type QrTokenResponse = {
  shiftId: string
  token: string
  expiresAt: string
}
