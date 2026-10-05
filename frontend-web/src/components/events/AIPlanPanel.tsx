import React, { useState } from 'react';
import {
  CheckCircle2,
  XCircle,
  Loader2,
  ChevronDown,
  ChevronRight,
  Sparkles,
  Clock,
  Wrench,
  User,
  Calendar,
  MapPin,
  Users,
  ShieldCheck,
  ShieldAlert,
  AlertTriangle,
} from 'lucide-react';
import type {
  WorkflowRunDetailDto,
  WorkflowRunStatus,
  WorkflowStateDto,
} from '../../types/agent';

interface Props {
  run: WorkflowRunDetailDto;
  reviewerId: string;
  onApprove: () => Promise<void>;
  onReject: (reason: string) => Promise<void>;
  isActioning: boolean;
  actionError: string | null;
}

export const AIPlanPanel: React.FC<Props> = ({
  run,
  onApprove,
  onReject,
  isActioning,
  actionError,
}) => {
  const [showRejectForm, setShowRejectForm] = useState(false);
  const [rejectReason, setRejectReason] = useState('');

  const workflow = parseWorkflowSummary(run.planSummary);
  const isPending = run.status === 'AwaitingApproval';

  const handleReject = async () => {
    if (rejectReason.trim().length === 0) return;
    await onReject(rejectReason.trim());
    setShowRejectForm(false);
    setRejectReason('');
  };

  return (
    <div className="rounded-2xl border border-slate-200 bg-white overflow-hidden">
      {/* Header */}
      <div className="px-5 py-4 bg-gradient-to-r from-brand-50 to-indigo-50 border-b border-slate-200 flex items-center justify-between">
        <div className="flex items-center">
          <Sparkles className="w-5 h-5 text-brand-600 mr-2" />
          <div>
            <div className="font-bold text-slate-800">Multi-Agent Staffing Plan</div>
            <div className="text-xs text-slate-500">
              Run ID: <span className="font-mono">{run.runId.slice(0, 8)}…</span>
              {workflow && (
                <> • <span className="font-mono">{workflow.agent_traces.length}</span> tool calls</>
              )}
            </div>
          </div>
        </div>
        <StatusBadge status={run.status} />
      </div>

      {/* Body */}
      <div className="p-5 space-y-4">
        {workflow ? (
          <>
            {/* Node 1: Planning */}
            <PlanningSection workflow={workflow} />

            {/* Node 2: Matching */}
            <MatchingSection workflow={workflow} />

            {/* Node 3: Scheduling */}
            <SchedulingSection workflow={workflow} />

            {/* Node 4: Validation */}
            <ValidationSection workflow={workflow} />

            {/* Audit trail */}
            <TracesSection workflow={workflow} />
          </>
        ) : (
          <div className="rounded-xl bg-amber-50 border border-amber-200 px-4 py-3 text-sm text-amber-800">
            This workflow run uses the older single-agent format. Trigger a new
            plan to see the full 4-agent output.
          </div>
        )}

        {/* Review notes */}
        {run.reviewNotes && (
          <div className="rounded-xl bg-slate-50 border border-slate-200 p-3">
            <div className="text-xs font-bold text-slate-500 uppercase tracking-wide mb-1">
              Review Notes
            </div>
            <div className="text-sm text-slate-700">{run.reviewNotes}</div>
            {run.reviewedByUserId && (
              <div className="text-[11px] text-slate-400 mt-1 font-mono">
                By: {run.reviewedByUserId.slice(0, 8)}…
              </div>
            )}
          </div>
        )}

        {/* Error */}
        {actionError && (
          <div className="rounded-xl bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            {actionError}
          </div>
        )}

        {/* Approve / Reject */}
        {isPending && !showRejectForm && (
          <div className="flex items-center justify-end space-x-2 pt-2 border-t border-slate-100">
            <button
              onClick={() => setShowRejectForm(true)}
              disabled={isActioning}
              className="flex items-center px-4 py-2 rounded-xl bg-white border border-red-200 text-red-600 text-sm font-semibold hover:bg-red-50 disabled:opacity-50 transition-colors"
            >
              <XCircle className="w-4 h-4 mr-1.5" />
              Reject
            </button>
            <button
              onClick={onApprove}
              disabled={isActioning}
              className="flex items-center px-4 py-2 rounded-xl bg-emerald-600 text-white text-sm font-semibold hover:bg-emerald-700 disabled:opacity-50 transition-colors shadow-sm shadow-emerald-500/20"
            >
              {isActioning ? (
                <>
                  <Loader2 className="w-4 h-4 mr-1.5 animate-spin" />
                  Processing...
                </>
              ) : (
                <>
                  <CheckCircle2 className="w-4 h-4 mr-1.5" />
                  Approve Plan
                </>
              )}
            </button>
          </div>
        )}

        {isPending && showRejectForm && (
          <div className="rounded-xl border border-red-200 bg-red-50/50 p-4 space-y-3">
            <div className="text-sm font-semibold text-red-800">Reject this plan</div>
            <textarea
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              placeholder="Explain why you're rejecting this plan (required)"
              rows={3}
              maxLength={1000}
              className="w-full px-3 py-2 rounded-lg border border-red-200 focus:border-red-500 focus:ring-2 focus:ring-red-500/20 outline-none text-sm resize-none bg-white"
            />
            <div className="flex items-center justify-end space-x-2">
              <button
                onClick={() => {
                  setShowRejectForm(false);
                  setRejectReason('');
                }}
                className="px-3 py-1.5 rounded-lg text-slate-600 text-sm hover:bg-white transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleReject}
                disabled={rejectReason.trim().length === 0 || isActioning}
                className="flex items-center px-4 py-2 rounded-lg bg-red-600 text-white text-sm font-semibold hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isActioning ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-1.5 animate-spin" />
                    Rejecting...
                  </>
                ) : (
                  'Confirm Reject'
                )}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

// ============================================================
// Section 1: PlanningAgent
// ============================================================
const PlanningSection: React.FC<{ workflow: WorkflowStateDto }> = ({ workflow }) => {
  const [open, setOpen] = useState(true);
  const steps = workflow.plan_steps ?? [];

  return (
    <div className="rounded-xl border border-blue-200 bg-blue-50/40">
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center justify-between px-4 py-3"
      >
        <div className="flex items-center">
          {open ? <ChevronDown className="w-4 h-4 mr-2" /> : <ChevronRight className="w-4 h-4 mr-2" />}
          <Sparkles className="w-4 h-4 text-blue-600 mr-2" />
          <span className="font-bold text-slate-800">Node 1 — PlanningAgent</span>
        </div>
        <span className="text-xs text-slate-500">{steps.length} steps</span>
      </button>

      {open && (
        <div className="px-4 pb-4 space-y-3">
          {workflow.event && (
            <div className="text-sm text-slate-700">
              <span className="font-semibold">{workflow.event.title}</span>
              {workflow.venue && (
                <span className="text-slate-500">
                  {' '}at {workflow.venue.name} (capacity {workflow.venue.capacity})
                </span>
              )}
            </div>
          )}

          {workflow.staffing_ratio && (
            <div className="text-sm text-slate-700">
              <span className="text-slate-500">Recommended:</span>{' '}
              <span className="font-semibold">
                {workflow.staffing_ratio.recommended_ushers} ushers
              </span>{' '}
              +{' '}
              <span className="font-semibold">
                {workflow.staffing_ratio.recommended_registration_staff} registration
              </span>{' '}
              ({workflow.staffing_ratio.total_staff} total)
            </div>
          )}

          {workflow.plan_reasoning && (
            <p className="text-xs text-slate-600 italic">{workflow.plan_reasoning}</p>
          )}

          {steps.length > 0 && (
            <div className="space-y-1.5">
              {steps.map((s) => (
                <div
                  key={s.step_number}
                  className="flex items-start rounded-lg bg-white px-3 py-2 text-xs"
                >
                  <div className="w-5 h-5 rounded-full bg-blue-600 text-white font-bold flex items-center justify-center flex-shrink-0 mt-0.5 text-[10px]">
                    {s.step_number}
                  </div>
                  <div className="ml-2 flex-1">
                    <div className="font-medium text-slate-700">{s.action}</div>
                    {s.tool && (
                      <div className="text-slate-400 flex items-center mt-0.5">
                        <Wrench className="w-3 h-3 mr-1" />
                        {s.tool}
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

// ============================================================
// Section 2: MatchingAgent
// ============================================================
const MatchingSection: React.FC<{ workflow: WorkflowStateDto }> = ({ workflow }) => {
  const [open, setOpen] = useState(false);
  const results = workflow.matching_results ?? [];

  return (
    <div className="rounded-xl border border-purple-200 bg-purple-50/40">
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center justify-between px-4 py-3"
      >
        <div className="flex items-center">
          {open ? <ChevronDown className="w-4 h-4 mr-2" /> : <ChevronRight className="w-4 h-4 mr-2" />}
          <Users className="w-4 h-4 text-purple-600 mr-2" />
          <span className="font-bold text-slate-800">Node 2 — MatchingAgent</span>
        </div>
        <span className="text-xs text-slate-500">
          {workflow.total_matched}/{workflow.total_headcount_needed} matched
        </span>
      </button>

      {open && (
        <div className="px-4 pb-4 space-y-2">
          {results.length === 0 ? (
            <p className="text-xs text-slate-500 italic">No matching results.</p>
          ) : (
            results.map((mr, i) => (
              <div key={i} className="rounded-lg bg-white p-3 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-semibold text-slate-700">{mr.role_name}</span>
                  <span className={statusTagColor(mr.status)}>{mr.status}</span>
                </div>
                {mr.error && (
                  <div className="text-xs text-red-600">Error: {mr.error}</div>
                )}
                {mr.matched_candidates.length > 0 ? (
                  <div className="space-y-1">
                    {mr.matched_candidates.map((c) => (
                      <div
                        key={c.volunteer_id}
                        className="flex items-center justify-between text-xs bg-slate-50 rounded px-2 py-1.5"
                      >
                        <div>
                          <span className="font-medium text-slate-700">{c.volunteer_name}</span>
                          <span className="text-slate-400 ml-2">
                            {c.experience_level} • ⭐ {c.rating_score.toFixed(1)}
                          </span>
                        </div>
                        <span className="font-mono text-purple-600">{c.match_score.toFixed(1)}</span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-xs text-slate-500 italic">
                    No candidates matched ({mr.unfulfilled_slots} slots unfulfilled).
                  </p>
                )}
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
};

// ============================================================
// Section 3: SchedulingAgent
// ============================================================
const SchedulingSection: React.FC<{ workflow: WorkflowStateDto }> = ({ workflow }) => {
  const [open, setOpen] = useState(false);
  const shifts = workflow.proposed_shifts ?? [];
  const conflicts = workflow.shift_conflicts ?? [];

  return (
    <div className="rounded-xl border border-amber-200 bg-amber-50/40">
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center justify-between px-4 py-3"
      >
        <div className="flex items-center">
          {open ? <ChevronDown className="w-4 h-4 mr-2" /> : <ChevronRight className="w-4 h-4 mr-2" />}
          <Calendar className="w-4 h-4 text-amber-600 mr-2" />
          <span className="font-bold text-slate-800">Node 3 — SchedulingAgent</span>
        </div>
        <span className="text-xs text-slate-500">
          {shifts.length} shifts • {conflicts.length} conflicts
        </span>
      </button>

      {open && (
        <div className="px-4 pb-4 space-y-3">
          {shifts.length === 0 ? (
            <p className="text-xs text-slate-500 italic">No shifts proposed.</p>
          ) : (
            <div className="space-y-1.5">
              {shifts.map((s) => {
                const assigned = s.assigned_candidates.length;
                return (
                  <div key={s.shift_id} className="rounded-lg bg-white p-3 text-xs">
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-semibold text-slate-700">{s.role_name}</span>
                      <span className="text-slate-500">
                        {assigned}/{s.capacity} assigned
                      </span>
                    </div>
                    <div className="text-slate-500">
                      {formatDateTime(s.start_time)} → {formatDateTime(s.end_time)}
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          {conflicts.length > 0 && (
            <div className="space-y-1">
              {conflicts.map((c, i) => (
                <div key={i} className={`text-xs rounded px-2 py-1.5 ${severityColor(c.severity)}`}>
                  <AlertTriangle className="w-3 h-3 inline mr-1" />
                  [{c.severity}] {c.message}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

// ============================================================
// Section 4: ValidationAgent
// ============================================================
const ValidationSection: React.FC<{ workflow: WorkflowStateDto }> = ({ workflow }) => {
  const [open, setOpen] = useState(true);
  const passed = workflow.validation_passed;
  const errors = workflow.validation_errors ?? [];
  const warnings = workflow.validation_warnings ?? [];

  return (
    <div className={`rounded-xl border ${passed ? 'border-emerald-200 bg-emerald-50/40' : 'border-red-200 bg-red-50/40'}`}>
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center justify-between px-4 py-3"
      >
        <div className="flex items-center">
          {open ? <ChevronDown className="w-4 h-4 mr-2" /> : <ChevronRight className="w-4 h-4 mr-2" />}
          {passed ? (
            <ShieldCheck className="w-4 h-4 text-emerald-600 mr-2" />
          ) : (
            <ShieldAlert className="w-4 h-4 text-red-600 mr-2" />
          )}
          <span className="font-bold text-slate-800">Node 4 — ValidationAgent</span>
        </div>
        <span className={`text-xs font-bold ${passed ? 'text-emerald-700' : 'text-red-700'}`}>
          {passed ? 'PASSED' : `${errors.length} ERROR${errors.length !== 1 ? 'S' : ''}`}
        </span>
      </button>

      {open && (
        <div className="px-4 pb-4 space-y-2">
          {errors.length > 0 && (
            <div className="space-y-1">
              {errors.map((e, i) => (
                <div key={i} className="text-xs text-red-700 flex items-start">
                  <XCircle className="w-3 h-3 mr-1.5 mt-0.5 flex-shrink-0" />
                  {e}
                </div>
              ))}
            </div>
          )}
          {warnings.length > 0 && (
            <div className="space-y-1">
              {warnings.map((w, i) => (
                <div key={i} className="text-xs text-amber-700 flex items-start">
                  <AlertTriangle className="w-3 h-3 mr-1.5 mt-0.5 flex-shrink-0" />
                  {w}
                </div>
              ))}
            </div>
          )}
          {passed && errors.length === 0 && warnings.length === 0 && (
            <p className="text-xs text-emerald-700">All validation rules passed.</p>
          )}
        </div>
      )}
    </div>
  );
};

// ============================================================
// Section 5: Audit trail
// ============================================================
const TracesSection: React.FC<{ workflow: WorkflowStateDto }> = ({ workflow }) => {
  const [open, setOpen] = useState(false);
  const traces = workflow.agent_traces ?? [];

  return (
    <div className="rounded-xl border border-slate-200 bg-white">
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center justify-between px-4 py-3"
      >
        <div className="flex items-center">
          {open ? <ChevronDown className="w-4 h-4 mr-2" /> : <ChevronRight className="w-4 h-4 mr-2" />}
          <Clock className="w-4 h-4 text-slate-600 mr-2" />
          <span className="font-bold text-slate-800">Audit Trail</span>
        </div>
        <span className="text-xs text-slate-500">{traces.length} entries</span>
      </button>

      {open && (
        <div className="px-4 pb-4 space-y-1">
          {traces.map((t, i) => (
            <div
              key={i}
              className="flex items-center justify-between text-xs bg-slate-50 rounded px-2 py-1.5"
            >
              <div className="flex items-center">
                <User className="w-3 h-3 mr-1.5 text-brand-500" />
                <span className="font-mono text-slate-600">{t.agent_name}</span>
                <Wrench className="w-3 h-3 mx-1.5 text-slate-400" />
                <span className="font-mono text-slate-500">{t.tool_name}</span>
              </div>
              <span className="text-slate-400 font-mono">{t.duration_ms}ms</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// ============================================================
// Status badge
// ============================================================
const StatusBadge: React.FC<{ status: WorkflowRunStatus }> = ({ status }) => {
  const style = (() => {
    switch (status) {
      case 'AwaitingApproval':
        return 'bg-amber-100 text-amber-800 border-amber-300';
      case 'Approved':
        return 'bg-emerald-100 text-emerald-800 border-emerald-300';
      case 'Rejected':
        return 'bg-red-100 text-red-800 border-red-300';
      case 'Running':
        return 'bg-blue-100 text-blue-800 border-blue-300';
      case 'Failed':
        return 'bg-slate-200 text-slate-700 border-slate-300';
      default:
        return 'bg-slate-100 text-slate-600 border-slate-200';
    }
  })();

  return <span className={`px-3 py-1 rounded-lg text-xs font-bold border ${style}`}>{status}</span>;
};

// ============================================================
// Helpers
// ============================================================
function parseWorkflowSummary(json: string | null): WorkflowStateDto | null {
  if (!json) return null;
  try {
    const parsed = JSON.parse(json);
    // Check it's the new shape (has agent_traces array)
    if (Array.isArray(parsed?.agent_traces)) {
      return parsed as WorkflowStateDto;
    }
    return null;
  } catch {
    return null;
  }
}

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function statusTagColor(status: string): string {
  switch (status) {
    case 'SUCCESS':
      return 'px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-100 text-emerald-800';
    case 'PARTIAL_MATCH':
      return 'px-2 py-0.5 rounded text-[10px] font-bold bg-amber-100 text-amber-800';
    case 'SAFE_FAILURE':
      return 'px-2 py-0.5 rounded text-[10px] font-bold bg-red-100 text-red-800';
    default:
      return 'px-2 py-0.5 rounded text-[10px] font-bold bg-slate-100 text-slate-700';
  }
}

function severityColor(sev: string): string {
  switch (sev) {
    case 'high':
      return 'bg-red-50 text-red-700 border border-red-200';
    case 'medium':
      return 'bg-amber-50 text-amber-700 border border-amber-200';
    default:
      return 'bg-slate-50 text-slate-600 border border-slate-200';
  }
}