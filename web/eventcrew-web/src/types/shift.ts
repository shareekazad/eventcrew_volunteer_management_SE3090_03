export type ShiftStatus = 'Open' | 'Full' | 'Completed' | 'Cancelled'

export type Shift = {
  id: string
  eventId: string
  roleRequirementId: string
  title: string
  startTime: string
  endTime: string
  capacity: number
  status: ShiftStatus
}

export type ShiftFormValues = {
  title: string
  eventId: string
  roleRequirementId: string
  date: string
  startTime: string
  endTime: string
  capacity: string
}