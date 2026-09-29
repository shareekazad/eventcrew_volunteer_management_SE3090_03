import { CalendarDays, Clock3, Pencil, Trash2, Users } from 'lucide-react'
import ShiftStatusBadge from './ShiftStatusBadge'
import type { Shift } from '../types/shift'

type ShiftCardProps = {
  shift: Shift
  eventName: (eventId: string) => string
  requirementName: string
  onEdit: (shift: Shift) => void
  onDelete: (shift: Shift) => void
}

export default function ShiftCard({ shift, eventName, requirementName, onEdit, onDelete }: ShiftCardProps) {
  const start = new Date(shift.startTime)
  const end = new Date(shift.endTime)

  return (
    <article className="shift-card">
      <div className="shift-card-heading">
        <div><p className="eyebrow">{requirementName}</p><h3>{shift.title}</h3></div>
        <ShiftStatusBadge status={shift.status} />
      </div>
      <p className="card-event"><CalendarDays size={15} aria-hidden="true" />{eventName(shift.eventId)}</p>
      <div className="card-details">
        <span><CalendarDays size={15} aria-hidden="true" />{start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}</span>
        <span><Clock3 size={15} aria-hidden="true" />{start.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })} – {end.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}</span>
        <span><Users size={15} aria-hidden="true" />{shift.capacity} volunteers</span>
      </div>
      <div className="card-actions">
        <button className="icon-button" type="button" aria-label={`Edit ${shift.title}`} onClick={() => onEdit(shift)}><Pencil size={16} /></button>
        <button className="icon-button danger-icon" type="button" aria-label={`Delete ${shift.title}`} onClick={() => onDelete(shift)}><Trash2 size={16} /></button>
      </div>
    </article>
  )
}