import { useEffect, useRef, useState, type FormEvent } from 'react'
import { X } from 'lucide-react'
import { getApiErrorMessage } from '../api/client'
import { getRoleRequirements, type RoleRequirementRecord } from '../api/roleRequirementService'
import type { EventRecord } from '../api/eventService'
import type { Shift, ShiftFormValues } from '../types/shift'

type ShiftModalProps = {
  shift?: Shift | null
  eventId: string
  events: EventRecord[]
  eventsLoading: boolean
  eventsError: string | null
  onRetryEvents: () => void
  onClose: () => void
  isSaving: boolean
  onSave: (values: ShiftFormValues, id?: string) => Promise<void>
}

type FormErrors = Partial<Record<keyof ShiftFormValues, string>>
const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

function toLocalInput(value?: string) {
  if (!value) return ''
  const date = new Date(value)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}

function initialValues(shift: Shift | null | undefined, eventId: string): ShiftFormValues {
  const start = toLocalInput(shift?.startTime)
  const end = toLocalInput(shift?.endTime)
  return {
    title: shift?.title ?? '', eventId: shift?.eventId ?? eventId,
    roleRequirementId: shift?.roleRequirementId ?? '', date: start ? start.slice(0, 10) : '',
    startTime: start ? start.slice(11, 16) : '', endTime: end ? end.slice(11, 16) : '',
    capacity: shift ? String(shift.capacity) : '',
  }
}

export default function ShiftModal({ shift, eventId, events, eventsLoading, eventsError, onRetryEvents, isSaving, onClose, onSave }: ShiftModalProps) {
  const [values, setValues] = useState(() => initialValues(shift, eventId))
  const [errors, setErrors] = useState<FormErrors>({})
  const [requirementResult, setRequirementResult] = useState<{ eventId: string; requirements?: RoleRequirementRecord[]; error?: string } | null>(null)
  const [requirementRetry, setRequirementRetry] = useState(0)
  const dialogRef = useRef<HTMLDivElement>(null)
  const titleRef = useRef<HTMLInputElement>(null)
  const isEditing = Boolean(shift)
  const currentRequirementResult = requirementResult?.eventId === values.eventId ? requirementResult : null
  const requirementOptions = currentRequirementResult?.requirements ?? []
  const isLoadingRequirements = guidPattern.test(values.eventId) && !currentRequirementResult

  useEffect(() => {
    if (!guidPattern.test(values.eventId)) return
    let isActive = true
    getRoleRequirements(values.eventId)
      .then((requirements) => {
        if (isActive) setRequirementResult({ eventId: values.eventId, requirements })
      })
      .catch((error: unknown) => {
        if (isActive) setRequirementResult({ eventId: values.eventId, error: getApiErrorMessage(error) })
      })
    return () => { isActive = false }
  }, [requirementRetry, values.eventId])

  useEffect(() => {
    const previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null
    titleRef.current?.focus()
    const handleKeyDown = (event: globalThis.KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
      if (event.key !== 'Tab' || !dialogRef.current) return
      const focusable = [...dialogRef.current.querySelectorAll<HTMLElement>('button, input, select, [tabindex]:not([tabindex="-1"])')]
      if (!focusable.length) return
      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
    }
    document.addEventListener('keydown', handleKeyDown)
    document.body.classList.add('dialog-open')
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      document.body.classList.remove('dialog-open')
      previouslyFocused?.focus()
    }
  }, [onClose])

  const update = (key: keyof ShiftFormValues, value: string) => {
    setValues((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const validate = () => {
    const next: FormErrors = {}
    if (!values.title.trim()) next.title = 'Enter a title for this shift.'
    if (!events.some((event) => event.id === values.eventId)) next.eventId = 'Choose an event.'
    if (!requirementOptions.some((requirement) => requirement.id === values.roleRequirementId)) next.roleRequirementId = 'Choose a role requirement for this event.'
    if (!values.date) next.date = 'Choose a date for this shift.'
    if (!values.startTime) next.startTime = 'Enter a start time.'
    if (!values.endTime) next.endTime = 'Enter an end time.'
    if (values.startTime && values.endTime && values.startTime >= values.endTime) next.endTime = 'End time must be later than start time.'
    if (!values.capacity || Number(values.capacity) < 1 || !Number.isInteger(Number(values.capacity))) next.capacity = 'Capacity must be a whole number greater than zero.'
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (validate()) await onSave(values, shift?.id)
  }
  return (
    <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
      <div className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="shift-modal-title" ref={dialogRef}>
        <div className="modal-heading"><div><p className="eyebrow">SHIFT DETAILS</p><h2 id="shift-modal-title">{isEditing ? 'Edit shift' : 'Create a shift'}</h2></div><button className="icon-button" type="button" aria-label="Close dialog" onClick={onClose}><X size={19} /></button></div>
        <form onSubmit={submit} noValidate>
          <div className="form-grid">
            <label className="form-field form-wide" htmlFor="shift-title">Shift title
              <input id="shift-title" ref={titleRef} value={values.title} maxLength={150} aria-invalid={Boolean(errors.title)} aria-describedby={errors.title ? 'title-error' : undefined} onChange={(event) => update('title', event.target.value)} placeholder="e.g. Guest check-in" />
              {errors.title && <span className="field-error" id="title-error">{errors.title}</span>}
            </label>
            <label className="form-field" htmlFor="shift-event">Event (required)
              <select id="shift-event" value={values.eventId} disabled={eventsLoading || !events.length} aria-invalid={Boolean(errors.eventId)} aria-describedby={errors.eventId ? 'event-error' : eventsError ? 'events-error' : undefined} onChange={(event) => { update('eventId', event.target.value); update('roleRequirementId', '') }}><option value="">{eventsLoading ? 'Loading events…' : 'Select event'}</option>{events.map((option) => <option key={option.id} value={option.id}>{option.title}</option>)}</select>
              {errors.eventId && <span className="field-error" id="event-error">{errors.eventId}</span>}
              {eventsError && <span className="field-error" id="events-error">{eventsError} <button className="inline-retry" type="button" onClick={onRetryEvents}>Retry</button></span>}
            </label>
            <label className="form-field" htmlFor="shift-requirement">Requirement (required)
              <select id="shift-requirement" value={values.roleRequirementId} disabled={!values.eventId || isLoadingRequirements || Boolean(currentRequirementResult?.error) || !requirementOptions.length} aria-invalid={Boolean(errors.roleRequirementId)} aria-describedby={errors.roleRequirementId ? 'requirement-error' : currentRequirementResult?.error ? 'requirements-error' : undefined} onChange={(event) => update('roleRequirementId', event.target.value)}><option value="">{isLoadingRequirements ? 'Loading requirements…' : 'Select requirement'}</option>{requirementOptions.map((option) => <option key={option.id} value={option.id}>{option.roleName} · {option.requiredHeadcount} required</option>)}</select>
              {errors.roleRequirementId && <span className="field-error" id="requirement-error">{errors.roleRequirementId}</span>}
              {currentRequirementResult?.error && <span className="field-error" id="requirements-error">{currentRequirementResult.error} <button className="inline-retry" type="button" onClick={() => { setRequirementResult(null); setRequirementRetry((current) => current + 1) }}>Retry</button></span>}
              {currentRequirementResult && !currentRequirementResult.error && !requirementOptions.length && <span className="field-hint">This event has no role requirements yet.</span>}
            </label>
            <label className="form-field" htmlFor="shift-date">Date
              <input id="shift-date" type="date" value={values.date} aria-invalid={Boolean(errors.date)} aria-describedby={errors.date ? 'date-error' : undefined} onChange={(event) => update('date', event.target.value)} />
              {errors.date && <span className="field-error" id="date-error">{errors.date}</span>}
            </label>
            <label className="form-field" htmlFor="shift-capacity">Capacity
              <input id="shift-capacity" type="number" min="1" step="1" inputMode="numeric" value={values.capacity} aria-invalid={Boolean(errors.capacity)} aria-describedby={errors.capacity ? 'capacity-error' : undefined} onChange={(event) => update('capacity', event.target.value)} placeholder="e.g. 8" />
              {errors.capacity && <span className="field-error" id="capacity-error">{errors.capacity}</span>}
            </label>
            <label className="form-field" htmlFor="shift-start">Start time
              <input id="shift-start" type="time" value={values.startTime} aria-invalid={Boolean(errors.startTime)} aria-describedby={errors.startTime ? 'start-error' : undefined} onChange={(event) => update('startTime', event.target.value)} />
              {errors.startTime && <span className="field-error" id="start-error">{errors.startTime}</span>}
            </label>
            <label className="form-field" htmlFor="shift-end">End time
              <input id="shift-end" type="time" value={values.endTime} aria-invalid={Boolean(errors.endTime)} aria-describedby={errors.endTime ? 'end-error' : undefined} onChange={(event) => update('endTime', event.target.value)} />
              {errors.endTime && <span className="field-error" id="end-error">{errors.endTime}</span>}
            </label>
          </div>
          <div className="modal-actions"><button className="button button-secondary" type="button" onClick={onClose} disabled={isSaving}>Cancel</button><button className="button button-primary" type="submit" disabled={isSaving}>{isSaving ? 'Saving…' : isEditing ? 'Save changes' : 'Create shift'}</button></div>
        </form>
      </div>
    </div>
  )
}