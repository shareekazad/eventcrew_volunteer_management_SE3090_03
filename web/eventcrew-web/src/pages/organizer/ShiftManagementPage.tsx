import { useEffect, useMemo, useState } from 'react'
import { CalendarDays, ChevronDown, ChevronRight, ClipboardList, Clock3, LayoutDashboard, Plus, Search, Settings, Users, X } from 'lucide-react'
import DeleteConfirmationDialog from '../../components/DeleteConfirmationDialog'
import ShiftCard from '../../components/ShiftCard'
import ShiftModal from '../../components/ShiftModal'
import ShiftTable from '../../components/ShiftTable'
import type { Shift, ShiftFormValues, ShiftStatus } from '../../types/shift'

const eventId = 'evt-community-arts'
const eventOptions = [{ id: eventId, name: 'Community Arts Festival' }]
const requirementOptions = [{ id: 'role-welcome', name: 'Welcome team' }, { id: 'role-logistics', name: 'Event logistics' }, { id: 'role-workshop', name: 'Workshop support' }]
const requirementNames = Object.fromEntries(requirementOptions.map((option) => [option.id, option.name]))
const eventName = eventOptions[0].name
const statuses: ShiftStatus[] = ['Open', 'Full', 'Completed', 'Cancelled']
const initialShifts: Shift[] = [
  { id: 'shift-1', eventId, roleRequirementId: 'role-welcome', title: 'Guest check-in', startTime: '2026-10-10T08:00:00', endTime: '2026-10-10T11:00:00', capacity: 6, status: 'Open' },
  { id: 'shift-2', eventId, roleRequirementId: 'role-logistics', title: 'Venue setup', startTime: '2026-10-10T07:00:00', endTime: '2026-10-10T10:30:00', capacity: 8, status: 'Full' },
  { id: 'shift-3', eventId, roleRequirementId: 'role-workshop', title: 'Kids art tent', startTime: '2026-10-10T10:00:00', endTime: '2026-10-10T13:00:00', capacity: 5, status: 'Open' },
  { id: 'shift-4', eventId, roleRequirementId: 'role-welcome', title: 'Information desk', startTime: '2026-10-10T13:00:00', endTime: '2026-10-10T16:00:00', capacity: 4, status: 'Completed' },
]

function ShiftTableSkeleton() {
  return <div className="skeleton-wrap" aria-label="Loading shifts" role="status"><span className="sr-only">Loading shifts…</span><div className="skeleton-header">{Array.from({ length: 9 }, (_, index) => <span className="skeleton-line" key={index} />)}</div>{Array.from({ length: 4 }, (_, row) => <div className="skeleton-row" key={row}>{Array.from({ length: 9 }, (_, col) => <span className="skeleton-line" key={col} />)}</div>)}</div>
}

export default function ShiftManagementPage() {
  const [shifts, setShifts] = useState(initialShifts)
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState('All statuses')
  const [isLoading, setIsLoading] = useState(true)
  const [editingShift, setEditingShift] = useState<Shift | null | undefined>(undefined)
  const [deletingShift, setDeletingShift] = useState<Shift | null>(null)

  useEffect(() => {
    const timer = window.setTimeout(() => setIsLoading(false), 650)
    return () => window.clearTimeout(timer)
  }, [])

  const filteredShifts = useMemo(() => {
    const query = searchTerm.trim().toLowerCase()
    return shifts.filter((shift) => {
      const matchesStatus = statusFilter === 'All statuses' || shift.status === statusFilter
      const matchesQuery = !query || [shift.title, eventName, requirementNames[shift.roleRequirementId] ?? ''].some((value) => value.toLowerCase().includes(query))
      return matchesStatus && matchesQuery
    })
  }, [searchTerm, shifts, statusFilter])

  const saveShift = (values: ShiftFormValues, id?: string) => {
    const startTime = new Date(`${values.date}T${values.startTime}`).toISOString()
    const endTime = new Date(`${values.date}T${values.endTime}`).toISOString()
    if (id) setShifts((current) => current.map((shift) => shift.id === id ? { ...shift, ...values, capacity: Number(values.capacity), startTime, endTime } : shift))
    else setShifts((current) => [...current, { ...values, id: `shift-${Date.now()}`, capacity: Number(values.capacity), startTime, endTime, status: 'Open' }])
    setEditingShift(undefined)
  }
  const startCreate = () => setEditingShift(null)
  const editShift = (shift: Shift) => setEditingShift(shift)
  const closeEditor = () => setEditingShift(undefined)
  const confirmDelete = () => {
    if (deletingShift) setShifts((current) => current.filter((shift) => shift.id !== deletingShift.id))
    setDeletingShift(null)
  }

  return (
    <div className="dashboard-shell">
      <aside className="sidebar">
        <a className="brand" href="#shifts" aria-label="EventCrew organizer home"><span className="brand-mark"><CalendarDays size={18} /></span><span>eventcrew<span className="brand-period">.</span></span></a>
        <div className="workspace-label">ORGANIZER WORKSPACE</div>
        <nav className="side-nav" aria-label="Organizer navigation"><a href="#overview"><LayoutDashboard size={18} />Overview</a><a href="#events"><CalendarDays size={18} />My events</a><a className="active" href="#shifts" aria-current="page"><Clock3 size={18} />Shift management</a><a href="#volunteers"><Users size={18} />Volunteers</a><a href="#requirements"><ClipboardList size={18} />Requirements</a></nav>
        <div className="sidebar-bottom"><a href="#settings"><Settings size={18} />Settings</a><div className="organizer-profile"><div className="avatar">JM</div><div><strong>Jordan Miller</strong><span>Event organizer</span></div><ChevronDown size={15} /></div></div>
      </aside>
      <main className="main-content" id="shifts">
        <header className="topbar"><div className="mobile-brand"><span className="brand-mark"><CalendarDays size={17} /></span>eventcrew<span className="brand-period">.</span></div><div className="topbar-context">Organizer workspace <ChevronRight size={15} /> Shift management</div><button className="topbar-avatar" type="button" aria-label="Organizer profile">JM</button></header>
        <div className="page-content">
          <div className="breadcrumb"><a href="#events">My events</a><ChevronRight size={14} /><span>{eventName}</span></div>
          <section className="page-heading"><div><p className="eyebrow">EVENT OPERATIONS</p><h1>Shift management</h1><p className="page-subtitle">Plan coverage and keep your event running smoothly.</p></div><button className="button button-primary create-button" type="button" onClick={startCreate}><Plus size={18} />Create Shift</button></section>
          <section className="event-banner" aria-label="Selected event"><div className="event-symbol"><CalendarDays size={20} /></div><div className="event-info"><span>MANAGING SHIFTS FOR</span><strong>{eventName}</strong></div><div className="event-date">OCT 10, 2026 <span>·</span> SATURDAY</div><button className="event-switch" type="button" aria-label="Change event">Change event <ChevronDown size={15} /></button></section>
          <section className="summary-row" aria-label="Shift summary"><div className="summary-item"><span className="summary-icon blue"><Clock3 size={17} /></span><div><span>Total shifts</span><strong>{shifts.length}</strong></div></div><div className="summary-item"><span className="summary-icon green"><Users size={17} /></span><div><span>Volunteer spots</span><strong>{shifts.reduce((sum, shift) => sum + shift.capacity, 0)}</strong></div></div><div className="summary-item"><span className="summary-icon amber"><CalendarDays size={17} /></span><div><span>Open shifts</span><strong>{shifts.filter((shift) => shift.status === 'Open').length}</strong></div></div></section>
          <section className="shift-section" aria-labelledby="shift-list-heading">
            <div className="section-heading"><div><h2 id="shift-list-heading">All shifts <span className="count-pill">{filteredShifts.length}</span></h2><p>Manage shift times, requirements, and volunteer capacity.</p></div></div>
            <div className="toolbar"><label className="search-field"><Search size={18} aria-hidden="true" /><span className="sr-only">Search shifts</span><input type="search" placeholder="Search shifts, roles..." value={searchTerm} onChange={(event) => setSearchTerm(event.target.value)} />{searchTerm && <button type="button" className="clear-search" aria-label="Clear search" onClick={() => setSearchTerm('')}><X size={15} /></button>}</label><label className="filter-field"><span className="sr-only">Filter by status</span><select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}><option>All statuses</option>{statuses.map((status) => <option key={status}>{status}</option>)}</select><ChevronDown size={15} aria-hidden="true" /></label><button className="button button-primary toolbar-create" type="button" onClick={startCreate}><Plus size={17} />Create Shift</button></div>
            {isLoading ? <ShiftTableSkeleton /> : filteredShifts.length ? <><div className="desktop-shifts"><ShiftTable shifts={filteredShifts} eventName={eventName} requirementNames={requirementNames} onEdit={editShift} onDelete={setDeletingShift} /></div><div className="mobile-shifts">{filteredShifts.map((shift) => <ShiftCard key={shift.id} shift={shift} eventName={eventName} requirementName={requirementNames[shift.roleRequirementId] ?? 'General volunteer'} onEdit={editShift} onDelete={setDeletingShift} />)}</div><div className="table-footer">Showing <strong>{filteredShifts.length}</strong> of <strong>{shifts.length}</strong> shifts <span>·</span> Updated just now</div></> : <div className="empty-state"><div className="empty-illustration"><CalendarDays size={28} /><span><Plus size={14} /></span></div><h3>{shifts.length ? 'No shifts match your search' : 'No shifts have been created for this event.'}</h3><p>{shifts.length ? 'Try a different search or status filter.' : 'Start building your event schedule by adding the first shift.'}</p>{shifts.length ? <button className="button button-secondary" type="button" onClick={() => { setSearchTerm(''); setStatusFilter('All statuses') }}>Clear filters</button> : <button className="button button-primary" type="button" onClick={startCreate}><Plus size={17} />Create Shift</button>}</div>}
          </section>
          <footer className="page-footer">EventCrew <span>·</span> Organizer tools</footer>
        </div>
      </main>
      {editingShift !== undefined && <ShiftModal shift={editingShift} eventId={eventId} eventOptions={eventOptions} requirementOptions={requirementOptions} onClose={closeEditor} onSave={saveShift} />}
      {deletingShift && <DeleteConfirmationDialog shiftTitle={deletingShift.title} onCancel={() => setDeletingShift(null)} onConfirm={confirmDelete} />}
    </div>
  )
}