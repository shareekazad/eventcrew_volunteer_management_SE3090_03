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
  Users,
  ShieldCheck,
  ShieldAlert,
  AlertTriangle,
  Info,
  Code2,
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
  const [showDevDetails, setShowDevDetails] = useState(false);

  const workflow = parseWorkflowSummary(run.planSummary);
  const isPending = run.status === 'AwaitingApproval';

  // Compute overall health for hero banner
  const errorCount = workflow?.validation_errors?.length ?? 0;
  const warningCount = workflow?.validation_warnings?.length ?? 0;
  const health: 'success' | 'warning' | 'failure' = !workflow
    ? 'warning'
    : run.status === 'Failed'
      ? 'failure'
      : errorCount > 0
        ? 'failure'
        : warningCount > 0
          ? 'warning'
          : 'success';

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
            {/* ⭐ NEW: Hero Summary */}
            <HeroSummary
              workflow={workflow}
              health={health}
              errorCount={errorCount}
              warningCount={warningCount}
            />

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

            {/* ⭐ NEW: Developer Details (collapsed by default) */}
            <DeveloperDetails
              run={run}
              workflow={workflow}
              open={showDevDetails}
              onToggle={() => setShowDevDetails((v) => !v)}
            />
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
// NEW: Hero Summary
// ============================================================
const HeroSummary: React.FC<{
  workflow: WorkflowStateDto;
  health: 'success' | 'warning' | 'failure';
  errorCount: number;
  warningCount: number;
}> = ({ workflow, health, errorCount, warningCount }) => {
  const tone = {
    success: {
      bg: 'bg-emerald-50 border-emerald-200',
      icon: <CheckCircle2 className="w-5 h-5 text-emerald-600" />,
      title: 'Plan generated successfully',
      titleColor: 'text-emerald-800',
    },
    warning: {
      bg: 'bg-amber-50 border-amber-200',
      icon: <AlertTriangle className="w-5 h-5 text-amber-600" />,
      title: 'Needs attention',
      titleColor: 'text-amber-800',
    },
    failure: {
      bg: 'bg-red-50 border-red-200',
      icon: <XCircle className="w-5 h-5 text-red-600" />,
      title: 'Plan has unresolved issues',
      titleColor: 'text-red-800',
    },
  }[health];

  const eventTitle = workflow.event?.title ?? 'This event';
  const venueName = workflow.venue?.name;
  const venueCapacity = workflow.venue?.capacity;
  const matched = workflow.total_matched ?? 0;
  const needed = workflow.total_headcount_needed ?? 0;

  // Build a plain-English sentence
  const sentences: string[] = [];

  if (venueName && venueCapacity) {
    sentences.push(`${eventTitle} at ${venueName} (capacity ${venueCapacity}).`);
  } else if (venueName) {
    sentences.push(`${eventTitle} at ${venueName}.`);
  } else {
    sentences.push(`${eventTitle}.`);
  }

  if (workflow.staffing_ratio) {
    sentences.push(
      `Recommended staffing: ${workflow.staffing_ratio.recommended_ushers} ushers + ${workflow.staffing_ratio.recommended_registration_staff} registration (${workflow.staffing_ratio.total_staff} total).`,
    );
  }

  if (needed > 0) {
    const tail: string[] = [];
    if (errorCount > 0) tail.push(`${errorCount} blocking issue${errorCount === 1 ? '' : 's'}`);
    if (warningCount > 0) tail.push(`${warningCount} warning${warningCount === 1 ? '' : 's'}`);

    if (tail.length === 0) {
      sentences.push(`All ${matched} of ${needed} volunteer slots matched and validated.`);
    } else {
      sentences.push(
        `Matched ${matched} of ${needed} volunteer slots. ${tail.join(' and ')} need${tail.length === 1 && errorCount + warningCount === 1 ? 's' : ''} your attention before approval.`,
      );
    }
  }

  return (
    <div className={`rounded-xl border ${tone.bg} p-4`}>
      <div className="flex items-start">
        <div className="mr-3 mt-0.5">{tone.icon}</div>
        <div className="flex-1">
          <div className={`font-bold ${tone.titleColor} mb-1`}>{tone.title}</div>
          <p className="text-sm text-slate-700 leading-relaxed">{sentences.join(' ')}</p>

          {/* Snapshot bullets */}
          <div className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1 text-xs text-slate-600">
            {workflow.event && (
              <div>
                <span className="text-slate-400">Event:</span>{' '}
                <span className="font-medium">{workflow.event.title}</span>
              </div>
            )}
            {workflow.venue && (
              <div>
                <span className="text-slate-400">Venue:</span>{' '}
                <span className="font-medium">
                  {workflow.venue.name}
                  {workflow.venue.capacity ? ` · ${workflow.venue.capacity} cap` : ''}
                </span>
              </div>
            )}
            {needed > 0 && (
              <div>
                <span className="text-slate-400">Matched:</span>{' '}
                <span className="font-medium">
                  {matched} of {needed}
                </span>
              </div>
            )}
            {(workflow.proposed_shifts?.length ?? 0) > 0 && (
              <div>
                <span className="text-slate-400">Shifts proposed:</span>{' '}
                <span className="font-medium">{workflow.proposed_shifts.length}</span>
              </div>
            )}
            <div>
              <span className="text-slate-400">Issues:</span>{' '}
              <span className="font-medium">
                {errorCount} error{errorCount === 1 ? '' : 's'}, {warningCount} warning
                {warningCount === 1 ? '' : 's'}
              </span>
            </div>
          </div>
        </div>
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
                  <span className={statusTagColor(mr.status)}>{humanStatus(mr.status)}</span>
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
                  <span className="font-semibold">{humanSeverity(c.severity)}:</span>{' '}
                  {c.message}
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
  const [showRaw, setShowRaw] = useState(false);
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
            <div className="space-y-1.5">
              {errors.map((e, i) => (
                <div key={i} className="text-xs text-red-700 flex items-start bg-white/60 rounded px-2 py-1.5">
                  <XCircle className="w-3.5 h-3.5 mr-1.5 mt-0.5 flex-shrink-0" />
                  <span>{humanizeMessage(e)}</span>
                </div>
              ))}
            </div>
          )}
          {warnings.length > 0 && (
            <div className="space-y-1.5">
              {warnings.map((w, i) => (
                <div key={i} className="text-xs text-amber-700 flex items-start bg-white/60 rounded px-2 py-1.5">
                  <AlertTriangle className="w-3.5 h-3.5 mr-1.5 mt-0.5 flex-shrink-0" />
                  <span>{humanizeMessage(w)}</span>
                </div>
              ))}
            </div>
          )}
          {passed && errors.length === 0 && warnings.length === 0 && (
            <p className="text-xs text-emerald-700">All validation rules passed.</p>
          )}

          {/* ⭐ NEW: Raw validator output toggle (nothing deleted) */}
          {(errors.length > 0 || warnings.length > 0) && (
            <div className="pt-1">
              <button
                onClick={() => setShowRaw((v) => !v)}
                className="text-[11px] text-slate-500 hover:text-slate-700 flex items-center"
              >
                {showRaw ? <ChevronDown className="w-3 h-3 mr-1" /> : <ChevronRight className="w-3 h-3 mr-1" />}
                <Code2 className="w-3 h-3 mr-1" />
                {showRaw ? 'Hide raw validator output' : 'Show raw validator output'}
              </button>
              {showRaw && (
                <div className="mt-2 rounded-lg bg-slate-900 text-slate-100 text-[11px] font-mono p-3 space-y-1 overflow-x-auto">
                  {errors.length > 0 && (
                    <div>
                      <div className="text-red-300 font-bold mb-1">// Errors ({errors.length})</div>
                      {errors.map((e, i) => (
                        <div key={i} className="whitespace-pre-wrap break-all">{e}</div>
                      ))}
                    </div>
                  )}
                  {warnings.length > 0 && (
                    <div className="mt-2">
                      <div className="text-amber-300 font-bold mb-1">// Warnings ({warnings.length})</div>
                      {warnings.map((w, i) => (
                        <div key={i} className="whitespace-pre-wrap break-all">{w}</div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>
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
  const totalMs = traces.reduce((sum, t) => sum + (t.duration_ms ?? 0), 0);

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
        <span className="text-xs text-slate-500">
          {traces.length} entries • {totalMs}ms total
        </span>
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
// NEW: Developer Details (collapsed by default — NOTHING deleted)
// ============================================================
const DeveloperDetails: React.FC<{
  run: WorkflowRunDetailDto;
  workflow: WorkflowStateDto;
  open: boolean;
  onToggle: () => void;
}> = ({ run, workflow, open, onToggle }) => {
  return (
    <div className="rounded-xl border border-slate-200 bg-slate-50/60">
      <button
        onClick={onToggle}
        className="w-full flex items-center justify-between px-4 py-3"
      >
        <div className="flex items-center">
          {open ? <ChevronDown className="w-4 h-4 mr-2" /> : <ChevronRight className="w-4 h-4 mr-2" />}
          <Code2 className="w-4 h-4 text-slate-600 mr-2" />
          <span className="font-bold text-slate-700 text-sm">Developer Details</span>
          <span className="ml-2 text-[11px] text-slate-400">
            (full IDs, raw state — for audit)
          </span>
        </div>
        <span className="text-xs text-slate-500">{open ? 'Hide' : 'Show'}</span>
      </button>

      {open && (
        <div className="px-4 pb-4 space-y-3 text-xs">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            <DevField label="Run ID" value={run.runId} mono />
            <DevField label="Status" value={run.status} />
            {workflow.event && <DevField label="Event ID" value={String(workflow.event.id)} mono />}
            {workflow.venue && <DevField label="Venue ID" value={String(workflow.venue.id)} mono />}
            {run.reviewedByUserId && (
              <DevField label="Reviewed by (user ID)" value={run.reviewedByUserId} mono />
            )}
            {run.reviewedAt && <DevField label="Reviewed at" value={run.reviewedAt} />}
          </div>

          {workflow.proposed_shifts && workflow.proposed_shifts.length > 0 && (
            <div>
              <div className="font-bold text-slate-600 mb-1">Shift IDs</div>
              <div className="space-y-0.5">
                {workflow.proposed_shifts.map((s) => (
                  <div key={s.shift_id} className="font-mono text-slate-600 break-all">
                    {s.shift_id} <span className="text-slate-400">— {s.role_name}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div>
            <div className="font-bold text-slate-600 mb-1">Raw workflow state (JSON)</div>
            <pre className="rounded-lg bg-slate-900 text-slate-100 p-3 overflow-x-auto text-[11px] leading-relaxed max-h-96 overflow-y-auto">
              {JSON.stringify(workflow, null, 2)}
            </pre>
          </div>

          <div className="text-[11px] text-slate-500 italic">
            <Info className="w-3 h-3 inline mr-1" />
            All data shown above is also displayed in the human-readable sections
            higher up — this is the raw audit form for reviewers.
          </div>
        </div>
      )}
    </div>
  );
};

const DevField: React.FC<{ label: string; value: string; mono?: boolean }> = ({
  label,
  value,
  mono,
}) => (
  <div>
    <span className="text-slate-400">{label}: </span>
    <span className={mono ? 'font-mono text-slate-700 break-all' : 'text-slate-700'}>{value}</span>
  </div>
);

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

// ⭐ NEW: human-friendly labels
function humanStatus(status: string): string {
  switch (status) {
    case 'SUCCESS':
      return 'Matched';
    case 'PARTIAL_MATCH':
      return 'Partial';
    case 'SAFE_FAILURE':
      return 'Failed safely';
    default:
      return status;
  }
}

function humanSeverity(sev: string): string {
  switch (sev) {
    case 'high':
      return 'Critical';
    case 'medium':
      return 'Warning';
    case 'low':
      return 'Info';
    default:
      return sev;
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

/**
 * Convert raw backend error strings into friendlier sentences.
 * Keeps the original text visible if the pattern isn't matched.
 * We do NOT delete anything — just prefix nicer phrasing where possible.
 */
function humanizeMessage(raw: string): string {
  // e.g. "Double-booking: Sarah Jenkins is assigned to overlapping shifts."
  if (/^double-booking:/i.test(raw)) {
    const rest = raw.replace(/^double-booking:\s*/i, '');
    return `Double-booking — ${rest}`;
  }
  if (/^understaffed:/i.test(raw)) {
    const rest = raw.replace(/^understaffed:\s*/i, '');
    return `Understaffed — ${rest}`;
  }
  if (/^candidate\s+/i.test(raw)) {
    return raw; // already readable
  }
  // Shift 838a5fc2 for 'Registration Desk' has 0/2 slots filled.
  const shiftMatch = raw.match(/^Shift\s+([0-9a-f-]+)\s+for\s+'([^']+)'\s+has\s+(.+)$/i);
  if (shiftMatch) {
    const [, shortId, role, rest] = shiftMatch;
    return `'${role}' shift (${shortId.slice(0, 8)}…) — ${rest}`;
  }
  return raw;
}