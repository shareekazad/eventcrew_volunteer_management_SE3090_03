export type ShiftAssignment = {
  id: string
  shiftId: string
  shiftTitle: string
  eventId: string
  eventName: string
  volunteerId: string
  volunteerName: string
  volunteerEmail: string
  status: string
  assignedAt: string
  createdAt: string
  updatedAt: string
}

export type EligibleVolunteer = {
  id: string
  fullName: string
  email: string
}

export type CreateShiftAssignmentRequest = {
  shiftId: string
  volunteerId: string
}