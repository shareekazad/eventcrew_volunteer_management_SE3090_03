import { useEffect, useMemo, useRef, useState } from 'react'
import { QRCodeSVG } from 'qrcode.react'
import { CalendarDays, ChevronDown, ChevronRight, ClipboardCheck, RefreshCw } from 'lucide-react'
import { generateShiftQrToken, getShiftAttendance } from '../../api/attendanceService'
import { getApiErrorMessage } from '../../api/client'
import { getAllEvents, type EventRecord } from '../../api/eventService'
import { getAllShifts } from '../../api/shiftService'
import OrganizerSidebar from '../../components/OrganizerSidebar'
import type { AttendanceRecord, QrTokenResponse } from '../../types/attendance'
import type { Shift } from '../../types/shift'

function formatDateTime(value: string | null) {
  return value ? new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' }) : '—'
}

function statusLabel(status: string) {
  if (status === 'Pending') return 'Not Checked In'
  if (status === 'CheckedIn') return 'Checked In'
  if (status === 'CheckedOut') return 'Checked Out'
  return status
}

export default function AttendanceManagementPage() {
  const [events, setEvents] = useState<EventRecord[]>([])
  const [shifts, setShifts] = useState<Shift[]>([])
  const [eventId, setEventId] = useState('')
  const [shiftId, setShiftId] = useState('')
  const [attendance, setAttendance] = useState<AttendanceRecord[]>([])
  const [qrToken, setQrToken] = useState<QrTokenResponse | null>(null)
  const [eventsLoading, setEventsLoading] = useState(true)
  const [shiftsLoading, setShiftsLoading] = useState(true)
  const [attendanceLoading, setAttendanceLoading] = useState(false)
  const [qrLoading, setQrLoading] = useState(false)
  const [eventsError, setEventsError] = useState<string | null>(null)
  const [shiftsError, setShiftsError] = useState<string | null>(null)
  const [attendanceError, setAttendanceError] = useState<string | null>(null)
  const [qrError, setQrError] = useState<string | null>(null)
  const attendanceRequestId = useRef(0)
  const eventShifts = useMemo(() => shifts.filter((shift) => shift.eventId === eventId), [shifts, eventId])
  const selectedShift = eventShifts.find((shift) => shift.id === shiftId)
  const selectedEvent = events.find((event) => event.id === eventId)

  useEffect(() => {
    let active = true
    getAllEvents().then((data) => { if (active) { setEvents(data); setEventsError(null) } })
      .catch((error: unknown) => { if (active) setEventsError(getApiErrorMessage(error)) })
      .finally(() => { if (active) setEventsLoading(false) })
    getAllShifts().then((data) => { if (active) { setShifts(data); setShiftsError(null) } })
      .catch((error: unknown) => { if (active) setShiftsError(getApiErrorMessage(error)) })
      .finally(() => { if (active) setShiftsLoading(false) })
    return () => { active = false }
  }, [])

  const loadAttendance = async (selectedShiftId: string) => {
    const requestId = ++attendanceRequestId.current
    setAttendanceLoading(true)
    setAttendanceError(null)
    setAttendance([])
    try {
      const data = await getShiftAttendance(selectedShiftId)
      if (requestId === attendanceRequestId.current) setAttendance(data)
    } catch (error) {
      if (requestId === attendanceRequestId.current) setAttendanceError(getApiErrorMessage(error))
    } finally {
      if (requestId === attendanceRequestId.current) setAttendanceLoading(false)
    }
  }

  const refreshAttendance = async () => {
    if (!shiftId) return
    await loadAttendance(shiftId)
  }

  const createQr = async () => {
    if (!shiftId) return
    setQrLoading(true)
    setQrError(null)
    setQrToken(null)
    try { setQrToken(await generateShiftQrToken(shiftId)) }
    catch (error) { setQrError(getApiErrorMessage(error)) }
    finally { setQrLoading(false) }
  }

  const contentError = eventsError || shiftsError

  return <div className="dashboard-shell">
    <OrganizerSidebar activePage="attendance" />
    <main className="main-content" id="attendance">
      <header className="topbar"><div className="mobile-brand"><span className="brand-mark"><CalendarDays size={17} /></span>eventcrew<span className="brand-period">.</span></div><div className="topbar-context">Organizer workspace <ChevronRight size={15} /> Attendance</div><button className="topbar-avatar" type="button" aria-label="Organizer profile">JM</button></header>
      <div className="page-content attendance-page">
        <div className="breadcrumb"><a href="#events">My events</a><ChevronRight size={14} /><span>Attendance</span></div>
        <section className="page-heading"><div><p className="eyebrow">VOLUNTEER CHECK-IN</p><h1>Attendance</h1><p className="page-subtitle">Generate a shift QR code and monitor volunteer check-ins.</p></div></section>

        <section className="attendance-selector" aria-label="Event and shift selection">
          <label className="attendance-field" htmlFor="attendance-event">Event
            <span className="attendance-select-wrap"><select id="attendance-event" value={eventId} disabled={eventsLoading || !!eventsError} onChange={(event) => { attendanceRequestId.current += 1; setEventId(event.target.value); setShiftId(''); setAttendance([]); setAttendanceLoading(false); setAttendanceError(null); setQrToken(null); setQrError(null) }}><option value="">{eventsLoading ? 'Loading events…' : 'Select an event'}</option>{events.map((event) => <option key={event.id} value={event.id}>{event.title}</option>)}</select><ChevronDown size={16} aria-hidden="true" /></span>
          </label>
          <label className="attendance-field" htmlFor="attendance-shift">Shift
            <span className="attendance-select-wrap"><select id="attendance-shift" value={shiftId} disabled={!eventId || shiftsLoading || !eventShifts.length} onChange={(event) => { const nextShiftId = event.target.value; setShiftId(nextShiftId); setQrToken(null); setAttendanceError(null); setQrError(null); if (nextShiftId) void loadAttendance(nextShiftId); else { attendanceRequestId.current += 1; setAttendance([]); setAttendanceLoading(false) } }}><option value="">{shiftsLoading ? 'Loading shifts…' : !eventId ? 'Select an event first' : eventShifts.length ? 'Select a shift' : 'No shifts for this event'}</option>{eventShifts.map((shift) => <option key={shift.id} value={shift.id}>{shift.title}</option>)}</select><ChevronDown size={16} aria-hidden="true" /></span>
          </label>
        </section>

        {contentError && <div className="api-error" role="alert"><strong>Attendance options couldn’t be loaded</strong><p>{contentError}</p><button className="button button-secondary" type="button" onClick={() => window.location.reload()}>Retry</button></div>}

        {!contentError && selectedShift && <>
          <section className="attendance-shift-card" aria-labelledby="attendance-shift-title">
            <div><p className="eyebrow">{selectedEvent?.title ?? selectedShift.eventName}</p><h2 id="attendance-shift-title">{selectedShift.title}</h2><p>{new Date(selectedShift.startTime).toLocaleString([], { dateStyle: 'full', timeStyle: 'short' })}</p></div>
            <div className="attendance-actions"><button className="button button-secondary" type="button" disabled={attendanceLoading} onClick={() => void refreshAttendance()}><RefreshCw size={15} className={attendanceLoading ? 'attendance-spinning' : undefined} />Refresh attendance</button><button className="button button-primary" type="button" disabled={qrLoading} onClick={() => void createQr()}><ClipboardCheck size={16} />{qrLoading ? 'Generating…' : 'Generate QR'}</button></div>
          </section>

          {qrError && <div className="api-error" role="alert"><strong>QR code couldn’t be generated</strong><p>{qrError}</p><button className="button button-secondary" type="button" onClick={() => void createQr()}>Try again</button></div>}
          {qrToken && <section className="attendance-qr-panel" aria-label="Shift attendance QR code"><div className="attendance-qr-image"><QRCodeSVG value={qrToken.token} size={256} level="H" includeMargin /></div><div className="attendance-qr-info"><span className="status-badge status-confirmed">QR active</span><h2>Scan to check in</h2><p>Show this code to volunteers assigned to <strong>{selectedShift.title}</strong>.</p><dl><div><dt>Shift</dt><dd>{selectedShift.title}</dd></div><div><dt>Expires</dt><dd>{formatDateTime(qrToken.expiresAt)}</dd></div></dl></div></section>}

          <section className="attendance-table-section" aria-labelledby="attendance-table-title">
            <div className="section-heading"><div><h2 id="attendance-table-title">Attendance records <span className="count-pill">{attendance.length}</span></h2><p>Only assignments with an attendance record are listed.</p></div></div>
            {attendanceError && <div className="api-error" role="alert"><strong>Attendance records couldn’t be loaded</strong><p>{attendanceError}</p><button className="button button-secondary" type="button" onClick={() => void refreshAttendance()}>Retry</button></div>}
            {!attendanceError && attendanceLoading ? <div className="assignment-loading" role="status">Loading attendance…</div> : !attendanceError && attendance.length ? <div className="table-scroll"><table className="attendance-table"><thead><tr><th>Volunteer</th><th>Assignment</th><th>Status</th><th>Check-in</th><th>Check-out</th><th>Verified hours</th></tr></thead><tbody>{attendance.map((record) => <tr key={record.id}><td className="attendance-volunteer">{record.volunteerName}<span>{record.volunteerEmail}</span></td><td>{record.shiftAssignmentId.slice(0, 8)}…</td><td><span className={`status-badge status-${record.status.toLowerCase()}`}>{statusLabel(record.status)}</span></td><td>{formatDateTime(record.checkInTime)}</td><td>{formatDateTime(record.checkOutTime)}</td><td>{Number(record.verifiedHours).toFixed(2)} h</td></tr>)}</tbody></table></div> : !attendanceError && <div className="attendance-empty"><ClipboardCheck size={23} /><strong>No attendance records yet</strong><span>Records appear after a volunteer checks in for this shift.</span></div>}
          </section>
        </>}

        {!contentError && !selectedShift && <div className="assignment-select-empty"><div className="empty-illustration"><ClipboardCheck size={27} /></div><h2>{eventsLoading || shiftsLoading ? 'Loading events and shifts…' : !eventId ? 'Select an event to begin' : eventShifts.length ? 'Select a shift to view attendance' : 'No shifts for this event'}</h2><p>Choose an event and shift to review attendance and generate its QR code.</p></div>}
        <footer className="page-footer">EventCrew <span>·</span> Organizer tools</footer>
      </div>
    </main>
  </div>
}
