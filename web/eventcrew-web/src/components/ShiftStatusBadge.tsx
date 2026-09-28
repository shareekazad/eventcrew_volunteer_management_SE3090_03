import type { ShiftStatus } from '../types/shift'

export default function ShiftStatusBadge({ status }: { status: ShiftStatus }) {
  return <span className={`status-badge status-${status.toLowerCase()}`}>{status}</span>
}