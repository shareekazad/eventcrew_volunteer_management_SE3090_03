import { useEffect, useMemo, useState } from 'react'
import { AlertTriangle, CheckCircle2, ChevronRight, Clock3, LoaderCircle, RefreshCw, Sparkles, UserRound, Users } from 'lucide-react'
import { getAllEvents, type EventRecord } from '../../api/eventService'
import { approveRoster, getWorkflowRun, planRoster, type PlannedRosterAssignment, type ValidationSummary } from '../../api/agentService'
import { getApiErrorMessage } from '../../api/client'
import OrganizerSidebar from '../../components/OrganizerSidebar'

function formatDateTime(value?: string | null) {
  if (!value) return '—'
  return new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
}

function parseRoster(raw: string | null | undefined): PlannedRosterAssignment[] {
  if (!raw) return []
  try {
    const parsed = JSON.parse(raw) as PlannedRosterAssignment[]
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function parseValidation(raw: string | null | undefined): ValidationSummary | null {
  if (!raw) return null
  try {
    const parsed = JSON.parse(raw) as ValidationSummary
    if (parsed && typeof parsed === 'object') return parsed
  } catch {
    return null
  }
  return null
}

export default function AISchedulerPage() {
  const [events, setEvents] = useState<EventRecord[]>([])
  const [selectedEventId, setSelectedEventId] = useState('')
  const [loadingEvents, setLoadingEvents] = useState(true)
  const [eventsError, setEventsError] = useState<string | null>(null)
  const [isGenerating, setIsGenerating] = useState(false)
  const [generateError, setGenerateError] = useState<string | null>(null)
  const [runId, setRunId] = useState<string | null>(null)
  const [roster, setRoster] = useState<PlannedRosterAssignment[]>([])
  const [validation, setValidation] = useState<ValidationSummary | null>(null)
  const [isApproving, setIsApproving] = useState(false)
  const [approvalState, setApprovalState] = useState<{ status: 'idle' | 'approved' | 'rejected'; message: string } | null>(null)

  useEffect(() => {
    let active = true
    getAllEvents()
      .then((data) => {
        if (!active) return
        setEvents(data)
        if (data[0]) setSelectedEventId(data[0].id)
      })
      .catch((error: unknown) => {
        if (!active) return
        setEventsError(getApiErrorMessage(error))
      })
      .finally(() => {
        if (active) setLoadingEvents(false)
      })

    return () => {
      active = false
    }
  }, [])

  const selectedEvent = useMemo(
    () => events.find((event) => event.id === selectedEventId) ?? events[0] ?? null,
    [events, selectedEventId],
  )

  const totalAssignments = roster.length
  const filledSlots = totalAssignments
  const unfilledSlots = validation?.total_unfilled_slots ?? 0
  const validationViolations = validation
    ? (validation.availability_violations ?? 0) +
      (validation.overlap_violations ?? 0) +
      (validation.role_requirement_violations ?? 0)
    : 0
  const hourLimitViolations = validation?.hour_limit_violations ?? 0
  const availabilityViolations = validation?.availability_violations ?? 0

  const generateRoster = async () => {
    if (!selectedEvent) return
    setIsGenerating(true)
    setGenerateError(null)
    setApprovalState(null)
    setRoster([])
    setValidation(null)
    setRunId(null)

    try {
      const response = await planRoster(selectedEvent.id)
      const nextRunId = response.runId
      if (!nextRunId) {
        throw new Error('The backend did not return a workflow run.')
      }

      const run = await getWorkflowRun(nextRunId)
      const nextRoster = parseRoster(run.generatedRosterProposal)
      const nextValidation = parseValidation(run.validationReport)
      setRunId(nextRunId)
      setRoster(nextRoster)
      setValidation(nextValidation)
    } catch (error: unknown) {
      setGenerateError(getApiErrorMessage(error))
    } finally {
      setIsGenerating(false)
    }
  }

  const approveRosterNow = async () => {
    if (!runId) return
    setIsApproving(true)
    setGenerateError(null)

    try {
      const updated = await approveRoster(runId)
      setApprovalState({
        status: 'approved',
        message: updated.status === 'Approved' ? 'Roster approved and assignments created.' : 'Roster approved.',
      })
      setValidation(parseValidation(updated.validationReport))
      setRoster(parseRoster(updated.generatedRosterProposal))
    } catch (error: unknown) {
      setApprovalState({ status: 'rejected', message: getApiErrorMessage(error) })
    } finally {
      setIsApproving(false)
    }
  }

  const showApprovalActions = Boolean(runId && !approvalState?.status)

  return (
    <div className="dashboard-shell">
      <OrganizerSidebar activePage="shifts" />
      <main className="main-content" id="ai-scheduler">
        <header className="topbar">
          <div className="mobile-brand"><span className="brand-mark"><Sparkles size={17} /></span>eventcrew<span className="brand-period">.</span></div>
          <div className="topbar-context"><ChevronRight size={15} /> Organizer workspace <ChevronRight size={15} /> AI Scheduler</div>
          <button className="topbar-avatar" type="button" aria-label="Organizer profile">JM</button>
        </header>

        <div className="page-content">
          <div className="breadcrumb"><a href="#shifts">Shift management</a><ChevronRight size={14} /><span>AI Scheduler</span></div>

          <section className="page-heading">
            <div>
              <p className="eyebrow">AI-POWERED STAFFING</p>
              <h1>AI Scheduler</h1>
              <p className="page-subtitle">Generate a roster from the existing EventCrew AI workflow and review the backend-generated assignment proposal.</p>
            </div>
          </section>

          <section className="assignment-selector" aria-label="AI scheduler controls">
            <label className="assignment-field" htmlFor="ai-event-select">
              Event
              <span className="select-wrap">
                <select
                  id="ai-event-select"
                  value={selectedEventId}
                  disabled={loadingEvents || !!eventsError || events.length === 0}
                  onChange={(event) => setSelectedEventId(event.target.value)}
                >
                  {events.map((event) => (
                    <option key={event.id} value={event.id}>{event.title}</option>
                  ))}
                </select>
              </span>
            </label>

            <div className="scheduler-actions">
              <button type="button" className="button-primary" onClick={generateRoster} disabled={!selectedEvent || isGenerating || loadingEvents}>
                {isGenerating ? <><LoaderCircle className="spinner" size={15} /> AI is generating the roster...</> : 'Generate Roster'}
              </button>
              {generateError && (
                <button type="button" className="button-secondary" onClick={generateRoster}>
                  <RefreshCw size={15} /> Retry
                </button>
              )}
            </div>
          </section>

          {eventsError && <div className="alert alert-error" role="alert">{eventsError}</div>}
          {generateError && <div className="alert alert-error" role="alert">{generateError}</div>}

          <div className="summary-row">
            <div className="summary-item"><span className="summary-icon"><Users size={17} /></span><div><span>Total assignments</span><strong>{totalAssignments}</strong></div></div>
            <div className="summary-item"><span className="summary-icon"><CheckCircle2 size={17} /></span><div><span>Filled slots</span><strong>{filledSlots}</strong></div></div>
            <div className="summary-item"><span className="summary-icon"><UserRound size={17} /></span><div><span>Unfilled slots</span><strong>{unfilledSlots}</strong></div></div>
            <div className="summary-item"><span className="summary-icon"><AlertTriangle size={17} /></span><div><span>Validation violations</span><strong>{validationViolations}</strong></div></div>
            <div className="summary-item"><span className="summary-icon"><Clock3 size={17} /></span><div><span>Hour-limit violations</span><strong>{hourLimitViolations}</strong></div></div>
            <div className="summary-item"><span className="summary-icon"><AlertTriangle size={17} /></span><div><span>Availability violations</span><strong>{availabilityViolations}</strong></div></div>
          </div>

          {approvalState && (
            <div className="status-banner status-success" role="status">
              <span>{approvalState.message}</span>
              {approvalState.status === 'approved' && (
                <a href="#assignments" className="button-secondary inline-button">View Assignments</a>
              )}
            </div>
          )}

          {showApprovalActions && (
            <div className="scheduler-controls">
              <button type="button" className="button-primary" onClick={approveRosterNow} disabled={isApproving || !runId}>
                {isApproving ? 'Approving...' : 'Approve Roster'}
              </button>
              <button type="button" className="button-secondary" onClick={() => setApprovalState({ status: 'rejected', message: 'Roster rejected.' })}>
                Reject Roster
              </button>
            </div>
          )}

          {isGenerating && (
            <div className="empty-state" role="status">
              <LoaderCircle className="spinner" size={28} />
              <h3>AI is generating the roster...</h3>
            </div>
          )}

          {!isGenerating && roster.length === 0 && !generateError && !runId && !eventsError && (
            <div className="empty-state">
              <h3>No roster yet</h3>
              <p>Select an event and generate a roster with the backend scheduling workflow.</p>
            </div>
          )}

          {roster.length > 0 && (
            <section className="table-card">
              <div className="section-heading">
                <h2>Generated roster proposal</h2>
                <p>Real backend AI scheduling output from the existing EventCrew agent.</p>
              </div>
              <div className="table-wrap">
                <table className="shift-table">
                  <thead>
                    <tr>
                      <th>Volunteer</th>
                      <th>Shift</th>
                      <th>Role</th>
                      <th>Start</th>
                      <th>End</th>
                      <th>Hours</th>
                    </tr>
                  </thead>
                  <tbody>
                    {roster.map((assignment, index) => (
                      <tr key={`${assignment.volunteer_id ?? 'vol'}-${assignment.shift_id ?? index}`}>
                        <td>{assignment.volunteer_name ?? 'Unknown volunteer'}</td>
                        <td>{assignment.shift_id ?? 'Unknown shift'}</td>
                        <td>{assignment.role ?? '—'}</td>
                        <td>{formatDateTime(assignment.shift_start)}</td>
                        <td>{formatDateTime(assignment.shift_end)}</td>
                        <td>{assignment.hours ?? 0}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          {validation && (
            <section className="table-card">
              <div className="section-heading">
                <h2>Scheduling summary</h2>
                <p>Validation and conflict details reported by the existing backend AI schedule.</p>
              </div>
              <ul className="detail-list">
                <li>Total assignments: {validation.total_assigned_volunteers ?? totalAssignments}</li>
                <li>Filled slots: {filledSlots}</li>
                <li>Unfilled slots: {validation.total_unfilled_slots ?? unfilledSlots}</li>
                <li>Validation violations: {validationViolations}</li>
                <li>Hour-limit violations: {hourLimitViolations}</li>
                <li>Availability violations: {availabilityViolations}</li>
                <li>Scheduling conflicts: {validation.overlap_violations ?? 0}</li>
              </ul>
            </section>
          )}
        </div>
      </main>
    </div>
  )
}
