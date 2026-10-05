import { useCallback, useEffect, useMemo, useState } from 'react'
import { ArrowRightLeft, CalendarDays, Check, ChevronRight, Clock3, Search, X } from 'lucide-react'
import {
  acceptShiftSwap,
  approveShiftSwap,
  cancelShiftSwap,
  createShiftSwap,
  declineShiftSwap,
  getAllShiftSwaps,
  rejectShiftSwapByOrganizer,
} from '../../api/shiftSwapService'
import { getAllShifts } from '../../api/shiftService'
import { getApiErrorMessage } from '../../api/client'
import OrganizerSidebar from '../../components/OrganizerSidebar'
import { useDemoRole } from '../../useDemoRole'
import type { ShiftSwapRequest } from '../../types/shiftSwap'
import type { Shift } from '../../types/shift'
import { previewShifts, previewSwaps } from '../dev/previewData'

type Toast = { kind: 'success' | 'error'; message: string }

function SwapSkeleton() {
  return (
    <div className="skeleton-wrap" aria-label="Loading shift swaps" role="status">
      <div className="skeleton-header">
        {Array.from({ length: 6 }, (_, index) => (
          <span className="skeleton-line" key={index} />
        ))}
      </div>
      {Array.from({ length: 3 }, (_, row) => (
        <div className="skeleton-row" key={row}>
          {Array.from({ length: 6 }, (_, col) => (
            <span className="skeleton-line" key={col} />
          ))}
        </div>
      ))}
    </div>
  )
}

export default function ShiftSwapsPage({ previewMode = false }: { previewMode?: boolean }) {
  const { role } = useDemoRole()
  const [swaps, setSwaps] = useState<ShiftSwapRequest[]>(() => previewMode ? previewSwaps : [])
  const [shifts, setShifts] = useState<Shift[]>(() => previewMode ? previewShifts : [])
  const [isLoading, setIsLoading] = useState(!previewMode)
  const [apiError, setApiError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState('All statuses')
  const [searchTerm, setSearchTerm] = useState('')
  const [toast, setToast] = useState<Toast | null>(null)
  const [actionInProgressId, setActionInProgressId] = useState<string | null>(null)

  // New Swap Request Modal State
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [reqAssignmentId, setReqAssignmentId] = useState('')
  const [targetVolunteerId, setTargetVolunteerId] = useState('')
  const [targetShiftId, setTargetShiftId] = useState('')
  const [reason, setReason] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const showToast = useCallback((kind: Toast['kind'], message: string) => setToast({ kind, message }), [])

  const fetchSwapsAndShifts = useCallback(async () => {
    if (previewMode) return
    try {
      const [swapsData, shiftsData] = await Promise.all([getAllShiftSwaps(), getAllShifts()])
      setSwaps(swapsData)
      setShifts(shiftsData)
      setApiError(null)
    } catch (error) {
      setApiError(getApiErrorMessage(error))
    } finally {
      setIsLoading(false)
    }
  }, [previewMode])

  useEffect(() => {
    if (previewMode) return
    let isActive = true
    Promise.all([getAllShiftSwaps(), getAllShifts()])
      .then(([swapsData, shiftsData]) => {
        if (!isActive) return
        setSwaps(swapsData)
        setShifts(shiftsData)
        setApiError(null)
      })
      .catch((error: unknown) => {
        if (isActive) setApiError(getApiErrorMessage(error))
      })
      .finally(() => {
        if (isActive) setIsLoading(false)
      })
    return () => {
      isActive = false
    }
  }, [previewMode])

  useEffect(() => {
    if (!toast) return
    const timer = window.setTimeout(() => setToast(null), 4500)
    return () => window.clearTimeout(timer)
  }, [toast])

  const filteredSwaps = useMemo(() => {
    const query = searchTerm.trim().toLowerCase()
    return swaps.filter((swap) => {
      const matchesStatus = statusFilter === 'All statuses' || swap.status === statusFilter
      const matchesQuery =
        !query ||
        [
          swap.requesterName,
          swap.targetVolunteerName,
          swap.sourceShiftTitle,
          swap.targetShiftTitle,
          swap.sourceEventTitle,
        ].some((val) => val.toLowerCase().includes(query))
      return matchesStatus && matchesQuery
    })
  }, [searchTerm, statusFilter, swaps])

  const isOrganizerOrAdmin = role === 'Organizer' || previewMode

  const handleApprove = async (id: string) => {
    setActionInProgressId(id)
    try {
      await approveShiftSwap(id)
      showToast('success', 'Shift swap request approved successfully.')
      await fetchSwapsAndShifts()
    } catch (error) {
      showToast('error', getApiErrorMessage(error))
    } finally {
      setActionInProgressId(null)
    }
  }

  const handleOrganizerReject = async (id: string) => {
    setActionInProgressId(id)
    try {
      await rejectShiftSwapByOrganizer(id)
      showToast('success', 'Shift swap request rejected.')
      await fetchSwapsAndShifts()
    } catch (error) {
      showToast('error', getApiErrorMessage(error))
    } finally {
      setActionInProgressId(null)
    }
  }

  const handleAccept = async (id: string) => {
    setActionInProgressId(id)
    try {
      await acceptShiftSwap(id)
      showToast('success', 'Swap request accepted! Pending organizer approval.')
      await fetchSwapsAndShifts()
    } catch (error) {
      showToast('error', getApiErrorMessage(error))
    } finally {
      setActionInProgressId(null)
    }
  }

  const handleDecline = async (id: string) => {
    setActionInProgressId(id)
    try {
      await declineShiftSwap(id)
      showToast('success', 'Swap request declined.')
      await fetchSwapsAndShifts()
    } catch (error) {
      showToast('error', getApiErrorMessage(error))
    } finally {
      setActionInProgressId(null)
    }
  }

  const handleCancel = async (id: string) => {
    setActionInProgressId(id)
    try {
      await cancelShiftSwap(id)
      showToast('success', 'Swap request cancelled.')
      await fetchSwapsAndShifts()
    } catch (error) {
      showToast('error', getApiErrorMessage(error))
    } finally {
      setActionInProgressId(null)
    }
  }

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (previewMode) return
    setIsSubmitting(true)
    try {
      await createShiftSwap({
        requesterAssignmentId: reqAssignmentId.trim(),
        targetVolunteerId: targetVolunteerId.trim(),
        targetShiftId: targetShiftId.trim(),
        reason: reason.trim() || undefined,
      })
      showToast('success', 'Shift swap request submitted.')
      setShowCreateModal(false)
      setReqAssignmentId('')
      setTargetVolunteerId('')
      setTargetShiftId('')
      setReason('')
      await fetchSwapsAndShifts()
    } catch (error) {
      showToast('error', getApiErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="dashboard-shell">
      <OrganizerSidebar activePage="swaps" previewMode={previewMode} />

      <main className="main-content" id="swaps">
        {previewMode && <div className="development-preview-notice" role="status"><strong>DEVELOPMENT PREVIEW — Authentication bypassed for UI testing</strong><span>Local demo data · changes are disabled.</span></div>}
        <header className="topbar">
          <div className="mobile-brand">
            <span className="brand-mark">
              <CalendarDays size={17} />
            </span>
            eventcrew<span className="brand-period">.</span>
          </div>
          <div className="topbar-context">
            Workspace <ChevronRight size={15} /> Shift Swaps
          </div>
          <button className="topbar-avatar" type="button" aria-label="User profile">
            {role === 'Organizer' || previewMode ? 'OR' : 'VO'}
          </button>
        </header>

        <div className="page-content">
          <div className="breadcrumb">
            <a href="#shifts">Shifts</a>
            <ChevronRight size={14} />
            <span>Shift Swaps</span>
          </div>

          <section className="page-heading">
            <div>
              <p className="eyebrow">ROSTERING & SWAPS</p>
              <h1>Shift Swaps</h1>
              <p className="page-subtitle">Manage volunteer shift swap requests and organizer approvals.</p>
            </div>
            {!isOrganizerOrAdmin && (
              <button
                className="button button-primary create-button"
                type="button"
                onClick={() => setShowCreateModal(true)}
              >
                <ArrowRightLeft size={18} /> Request Swap
              </button>
            )}
          </section>

          <section className="summary-row" aria-label="Shift swap summary">
            <div className="summary-item">
              <span className="summary-icon blue">
                <ArrowRightLeft size={17} />
              </span>
              <div>
                <span>Total requests</span>
                <strong>{swaps.length}</strong>
              </div>
            </div>
            <div className="summary-item">
              <span className="summary-icon amber">
                <Clock3 size={17} />
              </span>
              <div>
                <span>Pending organizer</span>
                <strong>{swaps.filter((s) => s.status === 'Pending_Organizer').length}</strong>
              </div>
            </div>
            <div className="summary-item">
              <span className="summary-icon green">
                <Check size={17} />
              </span>
              <div>
                <span>Approved swaps</span>
                <strong>{swaps.filter((s) => s.status === 'Approved').length}</strong>
              </div>
            </div>
          </section>

          <section className="shift-section" aria-labelledby="swap-list-heading">
            <div className="section-heading">
              <div>
                <h2 id="swap-list-heading">
                  Swap Requests <span className="count-pill">{filteredSwaps.length}</span>
                </h2>
                <p>Review incoming and outgoing shift swap requests.</p>
              </div>
            </div>

            <div className="toolbar">
              <label className="search-field">
                <Search size={18} aria-hidden="true" />
                <span className="sr-only">Search swaps</span>
                <input
                  type="search"
                  placeholder="Search by volunteer or shift title..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
                {searchTerm && (
                  <button
                    type="button"
                    className="clear-search"
                    aria-label="Clear search"
                    onClick={() => setSearchTerm('')}
                  >
                    <X size={15} />
                  </button>
                )}
              </label>
              <label className="filter-field">
                <span className="sr-only">Filter by status</span>
                <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                  <option>All statuses</option>
                  <option value="Pending_Target">Pending Target</option>
                  <option value="Pending_Organizer">Pending Organizer</option>
                  <option value="Approved">Approved</option>
                  <option value="Rejected">Rejected</option>
                  <option value="Cancelled">Cancelled</option>
                </select>
              </label>
            </div>

            {isLoading ? (
              <SwapSkeleton />
            ) : apiError ? (
              <div className="api-error" role="alert">
                <strong>Swaps could not be loaded</strong>
                <p>{apiError}</p>
                <button
                  className="button button-secondary"
                  type="button"
                  onClick={() => {
                    setIsLoading(true)
                    void fetchSwapsAndShifts()
                  }}
                >
                  Retry
                </button>
              </div>
            ) : filteredSwaps.length ? (
              <div className="desktop-shifts">
                <table className="shift-table" style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr>
                      <th style={{ textAlign: 'left', padding: '12px' }}>Requester</th>
                      <th style={{ textAlign: 'left', padding: '12px' }}>Source Shift</th>
                      <th style={{ textAlign: 'left', padding: '12px' }}>Target Volunteer</th>
                      <th style={{ textAlign: 'left', padding: '12px' }}>Target Shift</th>
                      <th style={{ textAlign: 'left', padding: '12px' }}>Reason</th>
                      <th style={{ textAlign: 'left', padding: '12px' }}>Status</th>
                      {!previewMode && <th style={{ textAlign: 'right', padding: '12px' }}>Actions</th>}
                    </tr>
                  </thead>
                  <tbody>
                    {filteredSwaps.map((swap) => {
                      const isBusy = actionInProgressId === swap.id
                      return (
                        <tr key={swap.id} style={{ borderBottom: '1px solid var(--border-color, #e5e7eb)' }}>
                          <td style={{ padding: '12px' }}>
                            <strong>{swap.requesterName}</strong>
                            <br />
                            <small className="muted-text">{swap.requesterEmail}</small>
                          </td>
                          <td style={{ padding: '12px' }}>
                            {swap.sourceShiftTitle}
                            <br />
                            <small className="muted-text">{swap.sourceEventTitle}</small>
                          </td>
                          <td style={{ padding: '12px' }}>
                            <strong>{swap.targetVolunteerName}</strong>
                            <br />
                            <small className="muted-text">{swap.targetVolunteerEmail}</small>
                          </td>
                          <td style={{ padding: '12px' }}>
                            {swap.targetShiftTitle}
                            <br />
                            <small className="muted-text">{swap.targetEventTitle}</small>
                          </td>
                          <td style={{ padding: '12px' }}>{swap.reason || '—'}</td>
                          <td style={{ padding: '12px' }}>
                            <span className={`status-badge status-${swap.status.toLowerCase().replace('_', '-')}`}>
                              {swap.status.replace('_', ' ')}
                            </span>
                          </td>
                          {!previewMode && <td style={{ padding: '12px', textAlign: 'right' }}>
                            <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                              {isOrganizerOrAdmin && swap.status === 'Pending_Organizer' && (
                                <>
                                  <button
                                    type="button"
                                    className="button button-primary"
                                    disabled={isBusy}
                                    onClick={() => void handleApprove(swap.id)}
                                  >
                                    Approve
                                  </button>
                                  <button
                                    type="button"
                                    className="button button-secondary"
                                    disabled={isBusy}
                                    onClick={() => void handleOrganizerReject(swap.id)}
                                  >
                                    Reject
                                  </button>
                                </>
                              )}

                              {!isOrganizerOrAdmin && swap.status === 'Pending_Target' && (
                                <>
                                  <button
                                    type="button"
                                    className="button button-primary"
                                    disabled={isBusy}
                                    onClick={() => void handleAccept(swap.id)}
                                  >
                                    Accept
                                  </button>
                                  <button
                                    type="button"
                                    className="button button-secondary"
                                    disabled={isBusy}
                                    onClick={() => void handleDecline(swap.id)}
                                  >
                                    Decline
                                  </button>
                                </>
                              )}

                              {(swap.status === 'Pending_Target' || swap.status === 'Pending_Organizer') && (
                                <button
                                  type="button"
                                  className="button button-secondary"
                                  disabled={isBusy}
                                  onClick={() => void handleCancel(swap.id)}
                                >
                                  Cancel
                                </button>
                              )}
                            </div>
                          </td>}
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="empty-state">
                <ArrowRightLeft size={28} />
                <h3>No shift swap requests found</h3>
                <p>Swap requests created by volunteers will appear here.</p>
              </div>
            )}
          </section>
        </div>
      </main>

      {!previewMode && showCreateModal && (
        <div className="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="create-swap-title">
          <div className="modal-card">
            <header className="modal-header">
              <h2 id="create-swap-title">Request Shift Swap</h2>
              <button
                type="button"
                className="close-button"
                aria-label="Close"
                onClick={() => setShowCreateModal(false)}
              >
                <X size={18} />
              </button>
            </header>
            <form onSubmit={(e) => void handleCreateSubmit(e)}>
              <div className="modal-body" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <label className="form-field">
                  <span>Source Assignment ID</span>
                  <input
                    type="text"
                    required
                    placeholder="Enter your assignment ID"
                    value={reqAssignmentId}
                    onChange={(e) => setReqAssignmentId(e.target.value)}
                  />
                </label>
                <label className="form-field">
                  <span>Target Volunteer Profile ID</span>
                  <input
                    type="text"
                    required
                    placeholder="Enter target volunteer ID"
                    value={targetVolunteerId}
                    onChange={(e) => setTargetVolunteerId(e.target.value)}
                  />
                </label>
                <label className="form-field">
                  <span>Target Shift ID</span>
                  <select
                    required
                    value={targetShiftId}
                    onChange={(e) => setTargetShiftId(e.target.value)}
                  >
                    <option value="">Select target shift...</option>
                    {shifts.map((shift) => (
                      <option key={shift.id} value={shift.id}>
                        {shift.title} ({shift.eventName})
                      </option>
                    ))}
                  </select>
                </label>
                <label className="form-field">
                  <span>Reason for swap (optional)</span>
                  <textarea
                    rows={3}
                    placeholder="Provide a brief reason..."
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                  />
                </label>
              </div>
              <footer className="modal-footer" style={{ marginTop: '20px', display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                <button
                  type="button"
                  className="button button-secondary"
                  onClick={() => setShowCreateModal(false)}
                >
                  Cancel
                </button>
                <button type="submit" className="button button-primary" disabled={isSubmitting}>
                  {isSubmitting ? 'Submitting...' : 'Submit Swap Request'}
                </button>
              </footer>
            </form>
          </div>
        </div>
      )}

      {toast && (
        <div className={`toast toast-${toast.kind}`} role={toast.kind === 'error' ? 'alert' : 'status'}>
          {toast.message}
        </div>
      )}
    </div>
  )
}
