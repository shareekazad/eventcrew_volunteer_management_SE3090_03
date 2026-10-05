import { useCallback, useEffect, useMemo, useState } from 'react'
import { CalendarDays, ChevronDown, ChevronRight, Clock3, Plus, Search, Users, X } from 'lucide-react'
import { createShift, deleteShift, getAllShifts, updateShift } from '../../api/shiftService'
import { getAllEvents, type EventRecord } from '../../api/eventService'
import { getApiErrorMessage } from '../../api/client'
import DeleteConfirmationDialog from '../../components/DeleteConfirmationDialog'
import ShiftCard from '../../components/ShiftCard'
import ShiftModal from '../../components/ShiftModal'
import ShiftTable from '../../components/ShiftTable'
import OrganizerSidebar from '../../components/OrganizerSidebar'
import type { Shift, ShiftFormValues, ShiftWriteRequest } from '../../types/shift'

type Toast = { kind: 'success' | 'error'; message: string }

function ShiftTableSkeleton() {
  return <div className="skeleton-wrap" aria-label="Loading shifts" role="status"><span className="sr-only">Loading shifts…</span><div className="skeleton-header">{Array.from({ length: 9 }, (_, index) => <span className="skeleton-line" key={index} />)}</div>{Array.from({ length: 4 }, (_, row) => <div className="skeleton-row" key={row}>{Array.from({ length: 9 }, (_, col) => <span className="skeleton-line" key={col} />)}</div>)}</div>
}

function toShiftRequest(values: ShiftFormValues): ShiftWriteRequest {
  return {
    title: values.title.trim(),
    eventId: values.eventId,
    roleRequirementId: values.roleRequirementId,
    startTime: new Date(`${values.date}T${values.startTime}`).toISOString(),
    endTime: new Date(`${values.date}T${values.endTime}`).toISOString(),
    capacity: Number(values.capacity),
  }
}

export default function ShiftManagementPage() {
  const [shifts, setShifts] = useState<Shift[]>([])
  const [events, setEvents] = useState<EventRecord[]>([])
  const [eventsLoading, setEventsLoading] = useState(true)
  const [eventsError, setEventsError] = useState<string | null>(null)
  const [eventsRetry, setEventsRetry] = useState(0)
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState('All statuses')
  const [isLoading, setIsLoading] = useState(true)
  const [apiError, setApiError] = useState<string | null>(null)
  const [editingShift, setEditingShift] = useState<Shift | null | undefined>(undefined)
  const [deletingShift, setDeletingShift] = useState<Shift | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [toast, setToast] = useState<Toast | null>(null)

  const loadShifts = useCallback(async () => {
    try {
      setShifts(await getAllShifts())
      setApiError(null)
      return true
    } catch (error) {
      setApiError(getApiErrorMessage(error))
      return false
    } finally { setIsLoading(false) }
  }, [])

  useEffect(() => {
    let isActive = true
    getAllShifts()
      .then((data) => {
        if (!isActive) return
        setShifts(data)
        setApiError(null)
      })
      .catch((error: unknown) => {
        if (isActive) setApiError(getApiErrorMessage(error))
      })
      .finally(() => {
        if (isActive) setIsLoading(false)
      })
    return () => { isActive = false }
  }, [])
  useEffect(() => {
    let isActive = true
    getAllEvents()
      .then((data) => {
        if (!isActive) return
        setEvents(data)
        setEventsError(null)
      })
      .catch((error: unknown) => {
        if (isActive) setEventsError(getApiErrorMessage(error))
      })
      .finally(() => {
        if (isActive) setEventsLoading(false)
      })
    return () => { isActive = false }
  }, [eventsRetry])
  useEffect(() => {
    if (!toast) return
    const timer = window.setTimeout(() => setToast(null), 4500)
    return () => window.clearTimeout(timer)
  }, [toast])

  const statuses = useMemo(() => [...new Set([...shifts.map((shift) => shift.status), ...(statusFilter === 'All statuses' ? [] : [statusFilter])])].sort(), [shifts, statusFilter])
  const filteredShifts = useMemo(() => {
    const query = searchTerm.trim().toLowerCase()
    return shifts.filter((shift) => {
      const matchesStatus = statusFilter === 'All statuses' || shift.status === statusFilter
      const matchesQuery = !query || [shift.title, shift.eventName, shift.roleRequirementName].some((value) => value.toLowerCase().includes(query))
      return matchesStatus && matchesQuery
    })
  }, [searchTerm, shifts, statusFilter])

  const showToast = useCallback((kind: Toast['kind'], message: string) => setToast({ kind, message }), [])
  const closeEditor = useCallback(() => setEditingShift(undefined), [])
  const cancelDelete = useCallback(() => setDeletingShift(null), [])

  const saveShift = useCallback(async (values: ShiftFormValues, id?: string) => {
    setIsSaving(true)
    try {
      const request = toShiftRequest(values)
      if (id) await updateShift(id, request)
      else await createShift(request)
      setEditingShift(undefined)
      const refreshed = await loadShifts()
      showToast('success', refreshed ? `Shift ${id ? 'updated' : 'created'}.` : 'Shift saved, but the list could not refresh.')
    } catch (error) {
      showToast('error', getApiErrorMessage(error))
    } finally {
      setIsSaving(false)
    }
  }, [loadShifts, showToast])

  const confirmDelete = useCallback(async () => {
    if (!deletingShift) return
    setIsDeleting(true)
    try {
      await deleteShift(deletingShift.id)
      setDeletingShift(null)
      const refreshed = await loadShifts()
      showToast('success', refreshed ? 'Shift deleted.' : 'Shift deleted, but the list could not refresh.')
    } catch (error) {
      showToast('error', getApiErrorMessage(error))
    } finally {
      setIsDeleting(false)
    }
  }, [deletingShift, loadShifts, showToast])

  const retryEvents = useCallback(() => {
    setEventsLoading(true)
    setEventsRetry((current) => current + 1)
  }, [])
  const selectedEventName = events.length > 1 ? `${events.length} events available` : events[0]?.title ?? 'No events available'

  return (
    <div className="dashboard-shell">
      <OrganizerSidebar activePage="shifts" />

      <main className="main-content" id="shifts">
        <header className="topbar"><div className="mobile-brand"><span className="brand-mark"><CalendarDays size={17} /></span>eventcrew<span className="brand-period">.</span></div><div className="topbar-context">Organizer workspace <ChevronRight size={15} /> Shift management</div><button className="topbar-avatar" type="button" aria-label="Organizer profile">JM</button></header>
        <div className="page-content">
          <div className="breadcrumb"><a href="#events">My events</a><ChevronRight size={14} /><span>All shifts</span></div>
          <section className="page-heading"><div><p className="eyebrow">EVENT OPERATIONS</p><h1>Shift management</h1><p className="page-subtitle">Plan coverage and keep your event running smoothly.</p></div><button className="button button-primary create-button" type="button" onClick={() => setEditingShift(null)}><Plus size={18} />Create Shift</button></section>
          <section className="event-banner" aria-label="Event data"><div className="event-symbol"><CalendarDays size={20} /></div><div className="event-info"><span>EVENTS AVAILABLE</span><strong>{selectedEventName}</strong></div><div className="event-date">{shifts.length} {shifts.length === 1 ? 'SHIFT' : 'SHIFTS'} LOADED</div></section>
          <section className="summary-row" aria-label="Shift summary"><div className="summary-item"><span className="summary-icon blue"><Clock3 size={17} /></span><div><span>Total shifts</span><strong>{shifts.length}</strong></div></div><div className="summary-item"><span className="summary-icon green"><Users size={17} /></span><div><span>Volunteer spots</span><strong>{shifts.reduce((sum, shift) => sum + shift.capacity, 0)}</strong></div></div><div className="summary-item"><span className="summary-icon amber"><CalendarDays size={17} /></span><div><span>Scheduled shifts</span><strong>{shifts.filter((shift) => shift.status === 'Scheduled').length}</strong></div></div></section>

          <section className="shift-section" aria-labelledby="shift-list-heading">
            <div className="section-heading"><div><h2 id="shift-list-heading">All shifts <span className="count-pill">{filteredShifts.length}</span></h2><p>Manage shift times, requirements, and volunteer capacity.</p></div></div>
            <div className="toolbar"><label className="search-field"><Search size={18} aria-hidden="true" /><span className="sr-only">Search shifts</span><input type="search" placeholder="Search shifts, roles..." value={searchTerm} onChange={(event) => setSearchTerm(event.target.value)} />{searchTerm && <button type="button" className="clear-search" aria-label="Clear search" onClick={() => setSearchTerm('')}><X size={15} /></button>}</label><label className="filter-field"><span className="sr-only">Filter by status</span><select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}><option>All statuses</option>{statuses.map((status) => <option key={status}>{status}</option>)}</select><ChevronDown size={15} aria-hidden="true" /></label><button className="button button-primary toolbar-create" type="button" onClick={() => setEditingShift(null)}><Plus size={17} />Create Shift</button></div>

            {isLoading ? <ShiftTableSkeleton /> : apiError ? <div className="api-error" role="alert"><strong>Shifts couldn’t be loaded</strong><p>{apiError}</p><button className="button button-secondary" type="button" onClick={() => { setIsLoading(true); void loadShifts() }}>Retry</button></div> : filteredShifts.length ? <><div className="desktop-shifts"><ShiftTable shifts={filteredShifts} onEdit={setEditingShift} onDelete={setDeletingShift} /></div><div className="mobile-shifts">{filteredShifts.map((shift) => <ShiftCard key={shift.id} shift={shift} onEdit={setEditingShift} onDelete={setDeletingShift} />)}</div><div className="table-footer">Showing <strong>{filteredShifts.length}</strong> of <strong>{shifts.length}</strong> shifts <span>·</span> Loaded from EventCrew API</div></> : <div className="empty-state"><div className="empty-illustration"><CalendarDays size={28} /><span><Plus size={14} /></span></div><h3>{shifts.length ? 'No shifts match your search' : 'No shifts have been created yet.'}</h3><p>{shifts.length ? 'Try a different search or status filter.' : 'Create a shift using an event and its role requirement.'}</p>{shifts.length ? <button className="button button-secondary" type="button" onClick={() => { setSearchTerm(''); setStatusFilter('All statuses') }}>Clear filters</button> : <button className="button button-primary" type="button" onClick={() => setEditingShift(null)}><Plus size={17} />Create Shift</button>}</div>}
          </section>
          <footer className="page-footer">EventCrew <span>·</span> Organizer tools</footer>
        </div>
      </main>

      {toast && <div className={`toast toast-${toast.kind}`} role={toast.kind === 'error' ? 'alert' : 'status'}>{toast.message}</div>}
      {editingShift !== undefined && <ShiftModal shift={editingShift} eventId={events[0]?.id ?? ''} events={events} eventsLoading={eventsLoading} eventsError={eventsError} onRetryEvents={retryEvents} isSaving={isSaving} onClose={closeEditor} onSave={saveShift} />}
      {deletingShift && <DeleteConfirmationDialog shiftTitle={deletingShift.title} isDeleting={isDeleting} onCancel={cancelDelete} onConfirm={() => void confirmDelete()} />}
    </div>
  )
}