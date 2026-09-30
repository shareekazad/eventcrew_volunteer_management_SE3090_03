export type AttendanceRecord = {
  id: string
  shiftAssignmentId: string
  shiftId: string
  shiftTitle: string
  volunteerId: string
  volunteerName: string
  volunteerEmail: string
  status: string
  checkInTime: string | null
  checkOutTime: string | null
  verifiedHours: number
  createdAt: string
  updatedAt: string
}

export type QrTokenResponse = {
  id: string
  shiftId: string
  token: string
  expiresAt: string
  isActive: boolean
  createdAt: string
}

export type AttendanceActionRequest = { token: string; shiftId: string; volunteerId: string }
