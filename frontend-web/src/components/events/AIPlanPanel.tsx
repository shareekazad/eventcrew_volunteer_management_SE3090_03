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
} from 'lucide-react';
import type {
  WorkflowRunDetailDto,
  WorkflowRunStatus,
  PlanResultDto,
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
  const [showPlan, setShowPlan] = useState(true);
  const [showRejectForm, setShowRejectForm] = useState(false);
  const [rejectReason, setRejectReason] = useState('');

  const plan = parsePlanSummary(run.planSummary);
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
            <div className="font-bold text-slate-800">AI Staffing Plan</div>
            <div className="text-xs text-slate-500">
              Run ID: <span className="font-mono">{run.runId.slice(0, 8)}…</span>
            </div>
          </div>
        </div>
        <StatusBadge status={run.status} />
      </div>

      {/* Body */}
      <div className="p-5 space-y-5">
        {/* Objective */}
        <div>
          <div className="text-xs font-bold text-slate-500 uppercase tracking-wide mb-1">
            Objective
          </div>
          <div className="text-sm text-slate-700">{run.objective}</div>
        </div>

        {/* Reasoning */}
        {plan && (
          <div className="rounded-xl bg-brand-50/40 border border-brand-100 p-4">
            <div className="text-xs font-bold text-brand-700 uppercase tracking-wide mb-1">
              Reasoning
            </div>
            <p className="text-sm text-slate-700 leading-relaxed">
              {plan.reasoning}
            </p>
          </div>
        )}

        {/* Plan steps (collapsible) */}
        {plan && (
          <div>
            <button
              onClick={() => setShowPlan((v) => !v)}
              className="flex items-center text-sm font-semibold text-slate-700 hover:text-brand-700 transition-colors"
            >
              {showPlan ? (
                <ChevronDown className="w-4 h-4 mr-1" />
              ) : (
                <ChevronRight className="w-4 h-4 mr-1" />
              )}
              Plan Steps ({plan.steps.length})
            </button>

            {showPlan && (
              <div className="mt-3 space-y-2">
                {plan.steps.map((step) => (
                  <div
                    key={step.step_number}
                    className="flex items-start rounded-xl bg-slate-50 px-4 py-2.5"
                  >
                    <div className="w-6 h-6 rounded-full bg-brand-600 text-white text-xs font-bold flex items-center justify-center flex-shrink-0 mt-0.5">
                      {step.step_number}
                    </div>
                    <div className="ml-3 flex-1">
                      <div className="text-sm font-medium text-slate-800">
                        {step.action}
                      </div>
                      <div className="text-xs text-slate-500 mt-0.5 flex items-center space-x-3">
                        <span className="flex items-center">
                          <User className="w-3 h-3 mr-1" />
                          {step.agent}
                        </span>
                        {step.tool && (
                          <span className="flex items-center">
                            <Wrench className="w-3 h-3 mr-1" />
                            {step.tool}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Tool calls (collapsible) */}
        {plan && plan.tool_calls.length > 0 && (
          <ToolCallsList toolCalls={plan.tool_calls} />
        )}

        {/* Review notes (if any) */}
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

        {/* Actions */}
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

        {/* Reject form */}
        {isPending && showRejectForm && (
          <div className="rounded-xl border border-red-200 bg-red-50/50 p-4 space-y-3">
            <div className="text-sm font-semibold text-red-800">
              Reject this plan
            </div>
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
// Helper components
// ============================================================

const ToolCallsList: React.FC<{ toolCalls: PlanResultDto['tool_calls'] }> = ({
  toolCalls,
}) => {
  const [expanded, setExpanded] = useState(false);

  return (
    <div>
      <button
        onClick={() => setExpanded((v) => !v)}
        className="flex items-center text-sm font-semibold text-slate-700 hover:text-brand-700 transition-colors"
      >
        {expanded ? (
          <ChevronDown className="w-4 h-4 mr-1" />
        ) : (
          <ChevronRight className="w-4 h-4 mr-1" />
        )}
        Tool Calls ({toolCalls.length})
      </button>

      {expanded && (
        <div className="mt-3 space-y-1.5">
          {toolCalls.map((call, idx) => (
            <div
              key={idx}
              className="flex items-center justify-between rounded-lg bg-slate-50 px-3 py-2 text-xs"
            >
              <div className="flex items-center font-mono text-slate-700">
                <Wrench className="w-3 h-3 mr-1.5 text-brand-500" />
                {call.tool_name}
              </div>
              <div className="flex items-center text-slate-400">
                <Clock className="w-3 h-3 mr-1" />
                {call.duration_ms} ms
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

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

  return (
    <span
      className={`px-3 py-1 rounded-lg text-xs font-bold border ${style}`}
    >
      {status}
    </span>
  );
};

// ============================================================
// Helpers
// ============================================================

function parsePlanSummary(json: string | null): PlanResultDto | null {
  if (!json) return null;
  try {
    return JSON.parse(json) as PlanResultDto;
  } catch {
    return null;
  }
}