import { useEffect, useRef, useState, type FormEvent } from 'react'
import { X } from 'lucide-react'
import type { Shift, ShiftFormValues } from '../types/shift'

type ShiftModalProps = {
  shift?: Shift | null
  eventId: string
  eventOptions: { id: string; name: string }[]
  requirementOptions: { id: string; name: string }[]
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

export default function ShiftModal({ shift, eventId, eventOptions, requirementOptions, isSaving, onClose, onSave }: ShiftModalProps) {
  const [values, setValues] = useState(() => initialValues(shift, eventId))
  const [errors, setErrors] = useState<FormErrors>({})
  const dialogRef = useRef<HTMLDivElement>(null)
  const titleRef = useRef<HTMLInputElement>(null)
  const isEditing = Boolean(shift)

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
    if (!guidPattern.test(values.eventId)) next.eventId = 'Enter a valid event ID.'
    if (!guidPattern.test(values.roleRequirementId)) next.roleRequirementId = 'Enter a valid role requirement ID.'
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
            <label className="form-field" htmlFor="shift-event">Event ID
              {eventOptions.length ? <select id="shift-event" value={values.eventId} aria-invalid={Boolean(errors.eventId)} aria-describedby={errors.eventId ? 'event-error' : undefined} onChange={(event) => update('eventId', event.target.value)}><option value="">Select event</option>{eventOptions.map((option) => <option key={option.id} value={option.id}>{option.name}</option>)}</select> : <input id="shift-event" value={values.eventId} aria-invalid={Boolean(errors.eventId)} aria-describedby={errors.eventId ? 'event-error' : undefined} onChange={(event) => update('eventId', event.target.value)} placeholder="Event UUID" />}
              {errors.eventId && <span className="field-error" id="event-error">{errors.eventId}</span>}
            </label>
            <label className="form-field" htmlFor="shift-requirement">Role requirement ID
              {requirementOptions.length ? <select id="shift-requirement" value={values.roleRequirementId} aria-invalid={Boolean(errors.roleRequirementId)} aria-describedby={errors.roleRequirementId ? 'requirement-error' : undefined} onChange={(event) => update('roleRequirementId', event.target.value)}><option value="">Select requirement</option>{requirementOptions.map((option) => <option key={option.id} value={option.id}>{option.name}</option>)}</select> : <input id="shift-requirement" value={values.roleRequirementId} aria-invalid={Boolean(errors.roleRequirementId)} aria-describedby={errors.roleRequirementId ? 'requirement-error' : undefined} onChange={(event) => update('roleRequirementId', event.target.value)} placeholder="Role requirement UUID" />}
              {errors.roleRequirementId && <span className="field-error" id="requirement-error">{errors.roleRequirementId}</span>}
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