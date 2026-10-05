import { Pencil, Trash2 } from 'lucide-react'
import ShiftStatusBadge from './ShiftStatusBadge'
import type { Shift } from '../types/shift'

type ShiftTableProps = {
  shifts: Shift[]
  onEdit: (shift: Shift) => void
  onDelete: (shift: Shift) => void
  readOnly?: boolean
}

export default function ShiftTable({ shifts, onEdit, onDelete, readOnly = false }: ShiftTableProps) {
  return (
    <div className="table-scroll">
      <table className="shift-table">
        <thead><tr><th>Shift title</th><th>Event</th><th>Requirement</th><th>Date</th><th>Start time</th><th>End time</th><th>Capacity</th><th>Status</th>{!readOnly && <th><span className="sr-only">Actions</span></th>}</tr></thead>
        <tbody>
          {shifts.map((shift) => {
            const start = new Date(shift.startTime)
            const end = new Date(shift.endTime)
            return (
              <tr key={shift.id}>
                <td className="title-cell">{shift.title}</td><td>{shift.eventName || shift.eventId}</td>
                <td>{shift.roleRequirementName || shift.roleRequirementId}</td>
                <td>{start.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}</td>
                <td>{start.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}</td><td>{end.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}</td>
                <td>{shift.capacity}</td><td><ShiftStatusBadge status={shift.status} /></td>
                {!readOnly && <td><div className="row-actions">
                  <button className="icon-button" type="button" aria-label={`Edit ${shift.title}`} onClick={() => onEdit(shift)}><Pencil size={16} /></button>
                  <button className="icon-button danger-icon" type="button" aria-label={`Delete ${shift.title}`} onClick={() => onDelete(shift)}><Trash2 size={16} /></button>
                </div></td>}
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}