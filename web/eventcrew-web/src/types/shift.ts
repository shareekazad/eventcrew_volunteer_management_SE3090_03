export type ShiftStatus = 'Draft' | 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled'

export type Shift = {
  id: string
  eventId: string
  roleRequirementId: string
  title: string
  eventName: string
  roleRequirementName: string
  startTime: string
  endTime: string
  capacity: number
  status: ShiftStatus
  createdAt: string
  updatedAt: string
}

export type ShiftWriteRequest = {
  title: string
  eventId: string
  roleRequirementId: string
  startTime: string
  endTime: string
  capacity: number
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