export type ShiftSwapStatus =
  | 'Pending_Target'
  | 'Pending_Organizer'
  | 'Approved'
  | 'Rejected'
  | 'Cancelled'

export type ShiftSwapRequest = {
  id: string
  requesterAssignmentId: string
  requesterVolunteerId: string
  requesterName: string
  requesterEmail: string
  sourceShiftId: string
  sourceShiftTitle: string
  sourceEventId: string
  sourceEventTitle: string
  targetVolunteerId: string
  targetVolunteerName: string
  targetVolunteerEmail: string
  targetShiftId: string
  targetShiftTitle: string
  targetEventId: string
  targetEventTitle: string
  reason: string | null
  status: ShiftSwapStatus
  createdAt: string
  updatedAt: string
}

export type CreateShiftSwapInput = {
  requesterAssignmentId: string
  targetVolunteerId: string
  targetShiftId: string
  reason?: string
}
