import type { ShiftStatus } from '../types/shift'

export default function ShiftStatusBadge({ status }: { status: ShiftStatus }) {
  const statusClass = status.toLowerCase().replace(/[^a-z0-9]+/g, '-')
  return <span className={`status-badge status-${statusClass}`}>{status}</span>
}