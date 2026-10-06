import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { QRCodeSVG } from 'qrcode.react'
import {
  CalendarDays,
  ChevronDown,
  ChevronRight,
  ClipboardCheck,
  Clock3,
  RefreshCw,
  Users,
  X,
} from 'lucide-react'
import {
  generateAttendanceQrToken,
  getAttendance,
  getAttendanceStatistics,
  getVolunteerAttendanceHistory,
} from '../../api/attendanceService'
import { getApiErrorMessage } from '../../api/client'
import { getAllEvents, type EventRecord } from '../../api/eventService'
import { getAllShifts } from '../../api/shiftService'
import OrganizerSidebar from '../../components/OrganizerSidebar'
import type { AttendanceRecord, AttendanceStatistics, QrTokenResponse } from '../../types/attendance'
import type { Shift } from '../../types/shift'

function formatDateTime(value: string | null) {
  return value
    ? new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
    : '—'
}

function statusLabel(status: AttendanceRecord['status']) {
  if (status === 'Pending') return 'Not checked in'
  if (status === 'CheckedIn') return 'Currently attending'
  if (status === 'CheckedOut') return 'Checked out'
  return status
}

function volunteerLabel(volunteerId: string) {
  return `Volunteer ${volunteerId.slice(0, 8)}`
}

export default function AttendanceManagementPage() {
  const [events, setEvents] = useState<EventRecord[]>([])
  const [shifts, setShifts] = useState<Shift[]>([])
  const [eventId, setEventId] = useState('')
  const [shiftId, setShiftId] = useState('')
  const [attendance, setAttendance] = useState<AttendanceRecord[]>([])
  const [statistics, setStatistics] = useState<AttendanceStatistics | null>(null)
  const [qrToken, setQrToken] = useState<QrTokenResponse | null>(null)
  const [eventsLoading, setEventsLoading] = useState(true)
  const [shiftsLoading, setShiftsLoading] = useState(true)
  const [attendanceLoading, setAttendanceLoading] = useState(false)
  const [qrLoading, setQrLoading] = useState(false)
  const [eventsError, setEventsError] = useState<string | null>(null)
  const [shiftsError, setShiftsError] = useState<string | null>(null)
  const [attendanceError, setAttendanceError] = useState<string | null>(null)
  const [qrError, setQrError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [historyVolunteerId, setHistoryVolunteerId] = useState<string | null>(null)
  const [history, setHistory] = useState<AttendanceRecord[]>([])
  const [historyLoading, setHistoryLoading] = useState(false)
  const [historyError, setHistoryError] = useState<string | null>(null)
  const [now, setNow] = useState(0)
  const attendanceRequestId = useRef(0)
  const historyRequestId = useRef(0)

  const eventShifts = useMemo(
    () => shifts.filter((shift) => shift.eventId === eventId),
    [shifts, eventId],
  )
  const selectedShift = eventShifts.find((shift) => shift.id === shiftId)
  const selectedEvent = events.find((event) => event.id === eventId)
  const qrExpired = qrToken !== null && Date.parse(qrToken.expiresAt) <= now
  const attendedCount = (statistics?.checkedIn ?? 0) + (statistics?.checkedOut ?? 0)
  const attendancePercentage = statistics?.total
    ? Math.round((attendedCount / statistics.total) * 100)
    : 0

  const closeHistory = useCallback(() => {
    historyRequestId.current += 1
    setHistoryVolunteerId(null)
    setHistory([])
    setHistoryError(null)
    setHistoryLoading(false)
  }, [])

  useEffect(() => {
    let active = true

    void getAllEvents()
      .then((data) => {
        if (active) {
          setEvents(data)
          setEventsError(null)
        }
      })
      .catch((error: unknown) => {
        if (active) setEventsError(getApiErrorMessage(error))
      })
      .finally(() => {
        if (active) setEventsLoading(false)
      })

    void getAllShifts()
      .then((data) => {
        if (active) {
          setShifts(data)
          setShiftsError(null)
        }
      })
      .catch((error: unknown) => {
        if (active) setShiftsError(getApiErrorMessage(error))
      })
      .finally(() => {
        if (active) setShiftsLoading(false)
      })

    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    if (!qrToken) return
    const timer = window.setInterval(() => setNow(Date.now()), 30_000)
    return () => window.clearInterval(timer)
  }, [qrToken])

  useEffect(() => {
    if (!historyVolunteerId) return
    document.body.classList.add('dialog-open')
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') closeHistory()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => {
      document.body.classList.remove('dialog-open')
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [historyVolunteerId, closeHistory])

  const loadOptions = async () => {
    setEventsLoading(true)
    setShiftsLoading(true)
    setEventsError(null)
    setShiftsError(null)
    const [eventsResult, shiftsResult] = await Promise.allSettled([getAllEvents(), getAllShifts()])

    if (eventsResult.status === 'fulfilled') setEvents(eventsResult.value)
    else setEventsError(getApiErrorMessage(eventsResult.reason))
    if (shiftsResult.status === 'fulfilled') setShifts(shiftsResult.value)
    else setShiftsError(getApiErrorMessage(shiftsResult.reason))
    setEventsLoading(false)
    setShiftsLoading(false)
  }

  const loadAttendance = async (nextEventId: string, nextShiftId: string) => {
    const requestId = ++attendanceRequestId.current
    if (!nextEventId) {
      setAttendance([])
      setStatistics(null)
      setAttendanceError(null)
      setAttendanceLoading(false)
      return
    }

    setAttendanceLoading(true)
    setAttendanceError(null)
    setNotice(null)
    try {
      const filters = { eventId: nextEventId, ...(nextShiftId ? { shiftId: nextShiftId } : {}) }
      const [records, summary] = await Promise.all([
        getAttendance(filters),
        getAttendanceStatistics(filters),
      ])
      if (requestId === attendanceRequestId.current) {
        setAttendance(records)
        setStatistics(summary)
      }
    } catch (error) {
      if (requestId === attendanceRequestId.current) setAttendanceError(getApiErrorMessage(error))
    } finally {
      if (requestId === attendanceRequestId.current) setAttendanceLoading(false)
    }
  }

  const refreshAttendance = () => loadAttendance(eventId, shiftId)

  const selectEvent = (nextEventId: string) => {
    attendanceRequestId.current += 1
    setEventId(nextEventId)
    setShiftId('')
    setAttendance([])
    setStatistics(null)
    setQrToken(null)
    setQrError(null)
    setAttendanceError(null)
    void loadAttendance(nextEventId, '')
  }

  const selectShift = (nextShiftId: string) => {
    attendanceRequestId.current += 1
    setShiftId(nextShiftId)
    setQrToken(null)
    setQrError(null)
    void loadAttendance(eventId, nextShiftId)
  }

  const createQr = async () => {
    if (!shiftId) return
    setQrLoading(true)
    setQrError(null)
    setNotice(null)
    setQrToken(null)
    try {
      const expiresAt = new Date(Date.now() + 15 * 60_000).toISOString()
      const token = await generateAttendanceQrToken(shiftId, expiresAt)
      setQrToken(token)
      setNow(Date.now())
      setNotice('A new attendance QR code is ready. It expires in 15 minutes.')
    } catch (error) {
      setQrError(getApiErrorMessage(error))
    } finally {
      setQrLoading(false)
    }
  }

  const openHistory = async (volunteerId: string) => {
    const requestId = ++historyRequestId.current
    setHistoryVolunteerId(volunteerId)
    setHistory([])
    setHistoryError(null)
    setHistoryLoading(true)
    try {
      const records = await getVolunteerAttendanceHistory(volunteerId)
      if (requestId === historyRequestId.current) setHistory(records)
    } catch (error) {
      if (requestId === historyRequestId.current) setHistoryError(getApiErrorMessage(error))
    } finally {
      if (requestId === historyRequestId.current) setHistoryLoading(false)
    }
  }

  const contentError = eventsError

  return <div className="dashboard-shell">
    <OrganizerSidebar activePage="attendance" />
    <main className="main-content" id="attendance">
      <header className="topbar">
        <div className="mobile-brand"><span className="brand-mark"><CalendarDays size={17} /></span>eventcrew<span className="brand-period">.</span></div>
        <div className="topbar-context">Organizer workspace <ChevronRight size={15} /> Attendance</div>
        <span className="topbar-avatar" aria-label="Organizer">{selectedEvent?.title.slice(0, 1) ?? 'E'}</span>
      </header>
      <div className="page-content attendance-page">
        <div className="breadcrumb"><a href="#events">My events</a><ChevronRight size={14} /><span>Attendance</span></div>
        <section className="page-heading">
          <div><p className="eyebrow">VOLUNTEER CHECK-IN</p><h1>Attendance</h1><p className="page-subtitle">Monitor event participation and manage shift check-in codes.</p></div>
        </section>

        <section className="attendance-selector" aria-label="Event and shift selection">
          <label className="attendance-field" htmlFor="attendance-event">Event
            <span className="attendance-select-wrap">
              <select
                id="attendance-event"
                value={eventId}
                disabled={eventsLoading || !!eventsError}
                onChange={(event) => selectEvent(event.target.value)}
              >
                <option value="">{eventsLoading ? 'Loading events…' : 'Select an event'}</option>
                {events.map((event) => <option key={event.id} value={event.id}>{event.title}</option>)}
              </select>
              <ChevronDown size={16} aria-hidden="true" />
            </span>
          </label>
          <label className="attendance-field" htmlFor="attendance-shift">Shift <span className="attendance-optional">optional</span>
            <span className="attendance-select-wrap">
              <select
                id="attendance-shift"
                value={shiftId}
                disabled={!eventId || shiftsLoading || !!shiftsError || !eventShifts.length}
                onChange={(event) => selectShift(event.target.value)}
              >
                <option value="">{shiftsLoading ? 'Loading shifts…' : !eventId ? 'Select an event first' : 'All shifts'}</option>
                {eventShifts.map((shift) => <option key={shift.id} value={shift.id}>{shift.title}</option>)}
              </select>
              <ChevronDown size={16} aria-hidden="true" />
            </span>
          </label>
        </section>

        {contentError && <div className="api-error" role="alert">
          <strong>Events couldn’t be loaded</strong><p>{contentError}</p>
          <button className="button button-secondary" type="button" onClick={() => void loadOptions()}>Retry</button>
        </div>}
        {shiftsError && eventId && <div className="api-error" role="alert">
          <strong>Shift options couldn’t be loaded</strong><p>{shiftsError}</p>
          <button className="button button-secondary" type="button" onClick={() => void loadOptions()}>Retry</button>
        </div>}

        {!contentError && eventId && <>
          <section className="attendance-shift-card" aria-labelledby="attendance-context-title">
            <div>
              <p className="eyebrow">{selectedEvent?.title ?? 'Selected event'}</p>
              <h2 id="attendance-context-title">{selectedShift?.title ?? 'All shifts'}</h2>
              <p>{selectedShift ? formatDateTime(selectedShift.startTime) : 'Event-wide attendance overview'}</p>
            </div>
            <div className="attendance-actions">
              <button className="button button-secondary" type="button" disabled={attendanceLoading} onClick={() => void refreshAttendance()}>
                <RefreshCw size={15} className={attendanceLoading ? 'attendance-spinning' : undefined} />Refresh
              </button>
              <button className="button button-primary" type="button" disabled={!shiftId || qrLoading} onClick={() => void createQr()}>
                <ClipboardCheck size={16} />{qrLoading ? 'Generating…' : qrToken ? 'Regenerate QR' : 'Generate QR'}
              </button>
            </div>
          </section>

          {notice && <div className="attendance-notice" role="status">{notice}</div>}
          {qrError && <div className="api-error" role="alert">
            <strong>QR code couldn’t be generated</strong><p>{qrError}</p>
            <button className="button button-secondary" type="button" onClick={() => void createQr()}>Try again</button>
          </div>}
          {qrToken && <section className="attendance-qr-panel" aria-label="Shift attendance QR code">
            <div className="attendance-qr-image"><QRCodeSVG value={qrToken.token} size={220} level="H" includeMargin /></div>
            <div className="attendance-qr-info">
              <span className={`status-badge ${qrExpired ? 'status-absent' : 'status-confirmed'}`}>{qrExpired ? 'QR expired' : 'QR active'}</span>
              <h2>{qrExpired ? 'Generate a new code' : 'Scan to check in'}</h2>
              <p>Display this code to volunteers assigned to <strong>{selectedShift?.title}</strong>. The token itself is not shown.</p>
              <dl><div><dt>Shift</dt><dd>{selectedShift?.title}</dd></div><div><dt>Expires</dt><dd>{formatDateTime(qrToken.expiresAt)}</dd></div></dl>
              {qrExpired && <button className="button button-primary attendance-regenerate" type="button" disabled={qrLoading} onClick={() => void createQr()}>Generate a fresh code</button>}
            </div>
          </section>}

          {attendanceError && <div className="api-error" role="alert">
            <strong>Attendance couldn’t be loaded</strong><p>{attendanceError}</p>
            <button className="button button-secondary" type="button" onClick={() => void refreshAttendance()}>Retry</button>
          </div>}

          {!attendanceError && statistics && <section className="attendance-stats" aria-label="Attendance statistics">
            <div className="attendance-stat"><span className="summary-icon blue"><Users size={17} /></span><div><span>Assigned / expected</span><strong>{statistics.total}</strong></div></div>
            <div className="attendance-stat"><span className="summary-icon green"><ClipboardCheck size={17} /></span><div><span>Checked in (total)</span><strong>{statistics.checkedIn + statistics.checkedOut}</strong></div></div>
            <div className="attendance-stat"><span className="summary-icon blue"><Clock3 size={17} /></span><div><span>Checked out</span><strong>{statistics.checkedOut}</strong></div></div>
            <div className="attendance-stat"><span className="summary-icon amber"><Clock3 size={17} /></span><div><span>Currently attending</span><strong>{statistics.checkedIn}</strong></div></div>
            <div className="attendance-stat"><span className="summary-icon amber"><Users size={17} /></span><div><span>Not checked in</span><strong>{statistics.pending + statistics.absent}</strong></div></div>
            <div className="attendance-stat"><span className="summary-icon blue"><CalendarDays size={17} /></span><div><span>Attendance rate</span><strong>{attendancePercentage}%</strong></div></div>
            <div className="attendance-stat"><span className="summary-icon green"><Clock3 size={17} /></span><div><span>Verified hours</span><strong>{statistics.verifiedHours.toFixed(2)} h</strong></div></div>
          </section>}

          <section className="attendance-table-section" aria-labelledby="attendance-table-title">
            <div className="section-heading">
              <div><h2 id="attendance-table-title">Attendance records <span className="count-pill">{attendance.length}</span></h2><p>Attendance status, volunteer participation, and verified hours.</p></div>
            </div>
            {!attendanceError && attendanceLoading
              ? <div className="assignment-loading" role="status">Loading attendance…</div>
              : !attendanceError && attendance.length
                ? <div className="table-scroll"><table className="attendance-table">
                  <thead><tr><th>Volunteer</th><th>Shift</th><th>Status</th><th>Check-in</th><th>Check-out</th><th>Verified hours</th><th>History</th></tr></thead>
                  <tbody>{attendance.map((record, index) => <tr key={record.id ?? `${record.volunteerId}-${record.shiftId}-${index}`}>
                    <td className="attendance-volunteer">{volunteerLabel(record.volunteerId)}<span>ID {record.volunteerId.slice(0, 8)}…</span></td>
                    <td>{record.shiftTitle}</td>
                    <td><span className={`status-badge status-${record.status.toLowerCase()}`}>{statusLabel(record.status)}</span></td>
                    <td>{formatDateTime(record.checkInTime)}</td>
                    <td>{formatDateTime(record.checkOutTime)}</td>
                    <td>{record.verifiedHours.toFixed(2)} h</td>
                    <td><button className="attendance-history-button" type="button" onClick={() => void openHistory(record.volunteerId)}>View history</button></td>
                  </tr>)}</tbody>
                </table></div>
                : !attendanceError && !attendanceLoading && <div className="attendance-empty">
                  <ClipboardCheck size={23} /><strong>No assigned volunteers yet</strong>
                  <span>Confirmed volunteer assignments appear here, including people who have not checked in.</span>
                </div>}
          </section>
        </>}

        {!contentError && !eventId && <div className="assignment-select-empty">
          <div className="empty-illustration"><ClipboardCheck size={27} /></div>
          <h2>{eventsLoading ? 'Loading events…' : 'Select an event to begin'}</h2>
          <p>Choose an event to review volunteer attendance and participation statistics.</p>
        </div>}
        <footer className="page-footer">EventCrew <span>·</span> Organizer tools</footer>
      </div>
    </main>

    {historyVolunteerId && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => {
      if (event.target === event.currentTarget) closeHistory()
    }}>
      <section className="modal-panel attendance-history-panel" role="dialog" aria-modal="true" aria-labelledby="history-title">
        <header className="attendance-history-header">
          <div><p className="eyebrow">VOLUNTEER PARTICIPATION</p><h2 id="history-title">{volunteerLabel(historyVolunteerId)}</h2></div>
          <button className="modal-close" type="button" aria-label="Close participation history" onClick={closeHistory}><X size={18} /></button>
        </header>
        {historyError && <div className="api-error" role="alert">
          <strong>Participation history couldn’t be loaded</strong><p>{historyError}</p>
          <button className="button button-secondary" type="button" onClick={() => void openHistory(historyVolunteerId)}>Retry</button>
        </div>}
        {!historyError && historyLoading && <div className="assignment-loading" role="status">Loading participation history…</div>}
        {!historyError && !historyLoading && history.length === 0 && <div className="attendance-empty">
          <ClipboardCheck size={23} /><strong>No participation history</strong><span>Completed or pending attendance records will appear here.</span>
        </div>}
        {!historyError && !historyLoading && history.length > 0 && <div className="attendance-history-list">
          {history.map((record, index) => <article className="attendance-history-item" key={record.id ?? `${record.eventId}-${record.shiftId}-${index}`}>
            <div><strong>{record.eventTitle}</strong><span>{record.shiftTitle}</span></div>
            <span className={`status-badge status-${record.status.toLowerCase()}`}>{statusLabel(record.status)}</span>
            <dl><div><dt>Check-in</dt><dd>{formatDateTime(record.checkInTime)}</dd></div><div><dt>Check-out</dt><dd>{formatDateTime(record.checkOutTime)}</dd></div><div><dt>Verified hours</dt><dd>{record.verifiedHours.toFixed(2)} h</dd></div></dl>
          </article>)}
        </div>}
        <div className="modal-actions"><button className="button button-secondary" type="button" onClick={closeHistory}>Close</button></div>
      </section>
    </div>}
  </div>
}
