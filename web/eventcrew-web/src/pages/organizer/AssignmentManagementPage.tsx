import { useEffect, useMemo, useState } from 'react'
import { CalendarDays, ChevronDown, ChevronRight, Clock3, UserPlus, Users, UserX } from 'lucide-react'
import { createAssignment, deleteAssignment, getAssignments, getEligibleVolunteers } from '../../api/assignmentService'
import { getApiErrorMessage } from '../../api/client'
import { getAllEvents, type EventRecord } from '../../api/eventService'
import { getAllShifts } from '../../api/shiftService'
import OrganizerSidebar from '../../components/OrganizerSidebar'
import type { EligibleVolunteer, ShiftAssignment } from '../../types/assignment'
import type { Shift } from '../../types/shift'

type Notice = { kind: 'success' | 'error'; message: string }

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, { weekday: 'short', month: 'long', day: 'numeric', year: 'numeric' })
}

export default function AssignmentManagementPage() {
  const [events, setEvents] = useState<EventRecord[]>([])
  const [shifts, setShifts] = useState<Shift[]>([])
  const [eventsLoading, setEventsLoading] = useState(true)
  const [shiftsLoading, setShiftsLoading] = useState(true)
  const [eventsError, setEventsError] = useState<string | null>(null)
  const [shiftsError, setShiftsError] = useState<string | null>(null)
  const [eventsRetry, setEventsRetry] = useState(0)
  const [shiftsRetry, setShiftsRetry] = useState(0)
  const [selectedEventId, setSelectedEventId] = useState('')
  const [selectedShiftId, setSelectedShiftId] = useState('')
  const [assignments, setAssignments] = useState<ShiftAssignment[]>([])
  const [eligibleVolunteers, setEligibleVolunteers] = useState<EligibleVolunteer[]>([])
  const [detailsLoading, setDetailsLoading] = useState(false)
  const [detailsError, setDetailsError] = useState<string | null>(null)
  const [refreshDetails, setRefreshDetails] = useState(0)
  const [volunteerId, setVolunteerId] = useState('')
  const [isAssigning, setIsAssigning] = useState(false)
  const [removingId, setRemovingId] = useState<string | null>(null)
  const [notice, setNotice] = useState<Notice | null>(null)

  useEffect(() => {
    let isActive = true
    getAllEvents()
      .then((data) => { if (isActive) { setEvents(data); setEventsError(null) } })
      .catch((error: unknown) => { if (isActive) setEventsError(getApiErrorMessage(error)) })
      .finally(() => { if (isActive) setEventsLoading(false) })
    return () => { isActive = false }
  }, [eventsRetry])

  useEffect(() => {
    let isActive = true
    getAllShifts()
      .then((data) => { if (isActive) { setShifts(data); setShiftsError(null) } })
      .catch((error: unknown) => { if (isActive) setShiftsError(getApiErrorMessage(error)) })
      .finally(() => { if (isActive) setShiftsLoading(false) })
    return () => { isActive = false }
  }, [shiftsRetry])

  const eventId = selectedEventId || events[0]?.id || ''
  const eventShifts = useMemo(() => shifts.filter((shift) => shift.eventId === eventId), [eventId, shifts])
  const selectedShift = eventShifts.find((shift) => shift.id === selectedShiftId)
  const isFull = Boolean(selectedShift && selectedShift.remainingCapacity <= 0)

  useEffect(() => {
    if (!selectedShift) return

    let isActive = true
    Promise.all([getAssignments({ shiftId: selectedShift.id }), getEligibleVolunteers(selectedShift.id)])
      .then(([assignmentData, volunteerData]) => {
        if (!isActive) return
        setAssignments(assignmentData.filter((assignment) => assignment.status === 'Confirmed' || assignment.status === 'Completed'))
        setEligibleVolunteers(volunteerData)
      })
      .catch((error: unknown) => { if (isActive) setDetailsError(getApiErrorMessage(error)) })
      .finally(() => { if (isActive) setDetailsLoading(false) })
    return () => { isActive = false }
  }, [refreshDetails, selectedShift])

  useEffect(() => {
    if (!notice) return
    const timeout = window.setTimeout(() => setNotice(null), 4500)
    return () => window.clearTimeout(timeout)
  }, [notice])

  const refreshShiftData = async () => {
    setDetailsLoading(true)
    setDetailsError(null)
    try {
      const data = await getAllShifts()
      setShifts(data)
      setShiftsError(null)
    } catch (error) {
      setShiftsError(getApiErrorMessage(error))
    }
    setRefreshDetails((current) => current + 1)
  }

  const assignVolunteer = async () => {
    if (!selectedShift || !volunteerId || isFull) return
    setIsAssigning(true)
    try {
      await createAssignment({ shiftId: selectedShift.id, volunteerId })
      setVolunteerId('')
      setNotice({ kind: 'success', message: 'Volunteer assigned to shift.' })
      await refreshShiftData()
    } catch (error) {
      setNotice({ kind: 'error', message: getApiErrorMessage(error) })
    } finally {
      setIsAssigning(false)
    }
  }

  const unassignVolunteer = async (assignment: ShiftAssignment) => {
    setRemovingId(assignment.id)
    try {
      await deleteAssignment(assignment.id)
      setNotice({ kind: 'success', message: `${assignment.volunteerName} was unassigned.` })
      await refreshShiftData()
    } catch (error) {
      setNotice({ kind: 'error', message: getApiErrorMessage(error) })
    } finally {
      setRemovingId(null)
    }
  }

  const selectedEvent = events.find((event) => event.id === eventId)
  const assignedCount = selectedShift?.assignedCount ?? assignments.length
  const capacity = selectedShift?.capacity ?? 0
  const remainingCapacity = selectedShift?.remainingCapacity ?? Math.max(capacity - assignedCount, 0)
  const capacityPercent = capacity ? Math.min((assignedCount / capacity) * 100, 100) : 0

  return (
    <div className="dashboard-shell">
      <OrganizerSidebar activePage="assignments" />
      <main className="main-content" id="assignments">
        <header className="topbar"><div className="mobile-brand"><span className="brand-mark"><CalendarDays size={17} /></span>eventcrew<span className="brand-period">.</span></div><div className="topbar-context">Organizer workspace <ChevronRight size={15} /> Assignment management</div><button className="topbar-avatar" type="button" aria-label="Organizer profile">JM</button></header>
        <div className="page-content">
          <div className="breadcrumb"><a href="#events">My events</a><ChevronRight size={14} /><span>Assignments</span></div>
          <section className="page-heading"><div><p className="eyebrow">VOLUNTEER ROSTERING</p><h1>Assignment management</h1><p className="page-subtitle">Place eligible volunteers into event shifts.</p></div></section>

          <section className="assignment-selector" aria-label="Event and shift selection">
            <label className="assignment-field" htmlFor="assignment-event">Event
              <span className="select-wrap"><select id="assignment-event" value={eventId} disabled={eventsLoading || !events.length} onChange={(event) => { setSelectedEventId(event.target.value); setSelectedShiftId(''); setVolunteerId(''); setAssignments([]); setEligibleVolunteers([]); setDetailsLoading(false); setDetailsError(null) }}><option value="">{eventsLoading ? 'Loading events…' : 'Select an event'}</option>{events.map((event) => <option key={event.id} value={event.id}>{event.title}</option>)}</select><ChevronDown size={16} aria-hidden="true" /></span>
              {eventsError && <span className="field-error" role="alert">{eventsError} <button className="inline-retry" type="button" onClick={() => { setEventsLoading(true); setEventsRetry((current) => current + 1) }}>Retry</button></span>}
            </label>
            <label className="assignment-field" htmlFor="assignment-shift">Shift
              <span className="select-wrap"><select id="assignment-shift" value={selectedShiftId} disabled={!eventId || shiftsLoading || !eventShifts.length} onChange={(event) => { setSelectedShiftId(event.target.value); setVolunteerId(''); setAssignments([]); setEligibleVolunteers([]); setDetailsError(null); setDetailsLoading(Boolean(event.target.value)) }}><option value="">{shiftsLoading ? 'Loading shifts…' : eventShifts.length ? 'Select a shift' : 'No shifts for this event'}</option>{eventShifts.map((shift) => <option key={shift.id} value={shift.id}>{shift.title}</option>)}</select><ChevronDown size={16} aria-hidden="true" /></span>
              {shiftsError && <span className="field-error" role="alert">{shiftsError} <button className="inline-retry" type="button" onClick={() => { setShiftsLoading(true); setShiftsRetry((current) => current + 1) }}>Retry</button></span>}
            </label>
          </section>

          {selectedShift ? <>
            <section className="selected-shift-panel" aria-labelledby="selected-shift-title">
              <div className="selected-shift-heading"><div><p className="eyebrow">{selectedEvent?.title ?? selectedShift.eventName}</p><h2 id="selected-shift-title">{selectedShift.title}</h2></div><span className={`status-badge status-${selectedShift.status.toLowerCase()}`}>{selectedShift.status}</span></div>
              <div className="selected-shift-facts"><span><CalendarDays size={16} aria-hidden="true" />{formatDate(selectedShift.startTime)}</span><span><Clock3 size={16} aria-hidden="true" />{formatTime(selectedShift.startTime)} – {formatTime(selectedShift.endTime)}</span><span><Users size={16} aria-hidden="true" />{selectedShift.roleRequirementName}</span></div>
              <div className="capacity-summary"><div className="capacity-numbers"><span>Assigned volunteers</span><strong>{assignedCount} <small>/ {capacity}</small></strong></div><div className="capacity-remaining"><span>{remainingCapacity}</span> {remainingCapacity === 1 ? 'spot remaining' : 'spots remaining'}</div><div className="capacity-track" role="progressbar" aria-label="Shift capacity" aria-valuemin={0} aria-valuemax={capacity} aria-valuenow={assignedCount}><span style={{ width: `${capacityPercent}%` }} /></div></div>
            </section>

                {detailsError && <div className="api-error" role="alert"><strong>Assignment details couldn’t be loaded</strong><p>{detailsError}</p><button className="button button-secondary" type="button" onClick={() => { setDetailsLoading(true); setDetailsError(null); setRefreshDetails((current) => current + 1) }}>Retry</button></div>}
            {!detailsError && <div className="assignment-columns">
              <section className="assignment-section" aria-labelledby="assigned-heading">
                <div className="section-heading"><div><h2 id="assigned-heading">Assigned volunteers <span className="count-pill">{assignedCount}</span></h2><p>Volunteers currently rostered for this shift.</p></div></div>
                {detailsLoading ? <div className="assignment-loading" role="status">Loading roster…</div> : assignments.length ? <ul className="assignment-list">{assignments.map((assignment) => <li className="assignment-person" key={assignment.id}><div className="person-avatar">{assignment.volunteerName.split(/\s+/).slice(0, 2).map((name) => name[0]).join('').toUpperCase()}</div><div className="person-info"><strong>{assignment.volunteerName}</strong><span>{assignment.volunteerEmail}</span></div><span className={`status-badge status-${assignment.status.toLowerCase()}`}>{assignment.status}</span><button className="unassign-button" type="button" disabled={removingId === assignment.id} onClick={() => void unassignVolunteer(assignment)} aria-label={`Unassign ${assignment.volunteerName}`}><UserX size={16} /><span>{removingId === assignment.id ? 'Removing…' : 'Unassign'}</span></button></li>)}</ul> : <div className="assignment-empty"><Users size={22} /><p>No volunteers assigned yet.</p></div>}
              </section>

              <section className="assignment-section assign-section" aria-labelledby="assign-heading">
                <div className="section-heading"><div><h2 id="assign-heading">Assign a volunteer</h2><p>Only volunteers with an eligible event application appear.</p></div></div>
                <label className="assignment-field" htmlFor="eligible-volunteer">Volunteer
                  <span className="select-wrap"><select id="eligible-volunteer" value={volunteerId} disabled={detailsLoading || isFull || !eligibleVolunteers.length} onChange={(event) => setVolunteerId(event.target.value)}><option value="">{detailsLoading ? 'Loading volunteers…' : isFull ? 'Shift is full' : eligibleVolunteers.length ? 'Select a volunteer' : 'No eligible volunteers'}</option>{eligibleVolunteers.map((volunteer) => <option key={volunteer.id} value={volunteer.id}>{volunteer.fullName} · {volunteer.email}</option>)}</select><ChevronDown size={16} aria-hidden="true" /></span>
                </label>
                {isFull && <p className="capacity-note">This shift is full. Unassign someone to make room.</p>}
                {!isFull && !detailsLoading && !eligibleVolunteers.length && <p className="capacity-note">No eligible volunteers are available for this shift.</p>}
                <button className="button button-primary assign-button" type="button" disabled={!volunteerId || isAssigning || isFull || detailsLoading} onClick={() => void assignVolunteer()}><UserPlus size={17} />{isAssigning ? 'Assigning…' : 'Assign Volunteer'}</button>
              </section>
            </div>}
          </> : <div className="assignment-select-empty"><div className="empty-illustration"><Users size={27} /></div><h2>{shiftsLoading ? 'Loading shifts…' : !eventId ? 'Select an event to begin' : shiftsError ? 'Shifts couldn’t be loaded' : 'Select a shift to manage its roster'}</h2><p>{shiftsError ? shiftsError : 'Choose an event and one of its shifts to view assignments and capacity.'}</p></div>}

          <footer className="page-footer">EventCrew <span>·</span> Organizer tools</footer>
        </div>
      </main>
      {notice && <div className={`toast toast-${notice.kind}`} role={notice.kind === 'error' ? 'alert' : 'status'}>{notice.message}</div>}
    </div>
  )
}