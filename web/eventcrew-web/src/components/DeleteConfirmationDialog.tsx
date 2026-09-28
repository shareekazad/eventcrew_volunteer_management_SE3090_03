import { useEffect, useRef } from 'react'
import { AlertTriangle, X } from 'lucide-react'

type DeleteConfirmationDialogProps = { shiftTitle: string; onCancel: () => void; onConfirm: () => void }

export default function DeleteConfirmationDialog({ shiftTitle, onCancel, onConfirm }: DeleteConfirmationDialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null)
  useEffect(() => {
    cancelRef.current?.focus()
    const handleKeyDown = (event: globalThis.KeyboardEvent) => { if (event.key === 'Escape') onCancel() }
    document.addEventListener('keydown', handleKeyDown)
    document.body.classList.add('dialog-open')
    return () => { document.removeEventListener('keydown', handleKeyDown); document.body.classList.remove('dialog-open') }
  }, [onCancel])

  return (
    <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) onCancel() }}>
      <section className="confirm-panel" role="alertdialog" aria-modal="true" aria-labelledby="delete-title" aria-describedby="delete-description">
        <button className="icon-button confirm-close" type="button" aria-label="Close dialog" onClick={onCancel}><X size={19} /></button>
        <div className="confirm-icon"><AlertTriangle size={22} aria-hidden="true" /></div>
        <h2 id="delete-title">Delete this shift?</h2>
        <p id="delete-description">“{shiftTitle}” will be removed from this event. This action can’t be undone.</p>
        <div className="modal-actions"><button ref={cancelRef} className="button button-secondary" type="button" onClick={onCancel}>Cancel</button><button className="button button-danger" type="button" onClick={onConfirm}>Delete Shift</button></div>
      </section>
    </div>
  )
}