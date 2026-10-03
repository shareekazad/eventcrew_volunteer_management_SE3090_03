import React, { useEffect } from 'react';
import {
  Bot,
  Cpu,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  ChevronDown,
  ChevronUp,
  Loader2,
  Sparkles,
  Star,
  ShieldCheck,
  RotateCcw,
  Clock,
  Hash,
  Zap,
  AlertCircle,
  Info,
  X,
} from 'lucide-react';
import {
  useMatchingStore,
  AGENT_EXECUTION_STEPS,
  AVAILABLE_ROLES,
  SKILL_OPTIONS,
} from '../stores/matchingStore';
import type { CandidateMatch, MatchingStatus } from '../types/matching';

// ─────────────────────────────────────────────────────────────────────────────
// Status badge helper
// ─────────────────────────────────────────────────────────────────────────────
const StatusBadge: React.FC<{ status: MatchingStatus }> = ({ status }) => {
  const config: Record<MatchingStatus, { label: string; cls: string; icon: React.ReactNode }> = {
    SUCCESS: {
      label: 'SUCCESS',
      cls: 'bg-emerald-100 text-emerald-800 border border-emerald-200',
      icon: <CheckCircle2 className="w-3.5 h-3.5" />,
    },
    PARTIAL_MATCH: {
      label: 'PARTIAL MATCH',
      cls: 'bg-amber-100 text-amber-800 border border-amber-200',
      icon: <AlertCircle className="w-3.5 h-3.5" />,
    },
    SAFE_FAILURE: {
      label: 'SAFE FAILURE',
      cls: 'bg-red-100 text-red-800 border border-red-200',
      icon: <AlertTriangle className="w-3.5 h-3.5" />,
    },
  };
  const c = config[status];
  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold ${c.cls}`}>
      {c.icon}
      {c.label}
    </span>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Rating stars
// ─────────────────────────────────────────────────────────────────────────────
const RatingStars: React.FC<{ rating: number }> = ({ rating }) => {
  return (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => (
        <Star
          key={star}
          className={`w-3.5 h-3.5 ${
            star <= Math.round(rating)
              ? 'text-amber-400 fill-amber-400'
              : 'text-slate-200 fill-slate-200'
          }`}
        />
      ))}
      <span className="ml-1 text-xs text-slate-600 font-semibold">{rating.toFixed(1)}</span>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Candidate Card
// ─────────────────────────────────────────────────────────────────────────────
const CandidateCard: React.FC<{ candidate: CandidateMatch; rank: number }> = ({
  candidate,
  rank,
}) => {
  const rankColors = ['from-amber-500 to-yellow-400', 'from-slate-400 to-slate-300', 'from-amber-700 to-orange-500'];
  const rankLabels = ['1st', '2nd', '3rd'];
  const scoreColor =
    candidate.match_score >= 90
      ? 'bg-emerald-500'
      : candidate.match_score >= 70
      ? 'bg-brand-500'
      : candidate.match_score >= 50
      ? 'bg-amber-500'
      : 'bg-red-500';

  const initials = candidate.volunteer_name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  return (
    <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm hover:shadow-md transition-shadow duration-200 group">
      <div className="flex items-start justify-between gap-3 mb-4">
        <div className="flex items-center gap-3">
          {/* Rank badge */}
          <div
            className={`w-8 h-8 rounded-full bg-gradient-to-br ${rankColors[Math.min(rank - 1, 2)] ?? 'from-slate-300 to-slate-400'} flex items-center justify-center text-white font-bold text-xs shadow-sm`}
          >
            {rank <= 3 ? rankLabels[rank - 1] : `#${rank}`}
          </div>
          {/* Avatar */}
          <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-brand-600 to-indigo-500 text-white font-bold text-sm flex items-center justify-center shadow-sm">
            {initials}
          </div>
          <div>
            <h3 className="font-bold text-slate-900 text-sm leading-tight">
              {candidate.volunteer_name}
            </h3>
            <p className="text-xs text-slate-500">{candidate.experience_level} Level</p>
          </div>
        </div>

        {/* Match score pill */}
        <div className="flex flex-col items-end gap-1.5">
          <span className="text-xs font-bold text-slate-500">Match Score</span>
          <div className="flex items-center gap-2">
            <div className="w-20 h-2 rounded-full bg-slate-100 overflow-hidden">
              <div
                className={`h-full rounded-full transition-all duration-700 ${scoreColor}`}
                style={{ width: `${candidate.match_score}%` }}
              />
            </div>
            <span
              className={`px-2 py-0.5 rounded-full text-xs font-bold text-white ${scoreColor}`}
            >
              {candidate.match_score.toFixed(0)}%
            </span>
          </div>
        </div>
      </div>

      {/* Rating */}
      <div className="mb-3">
        <RatingStars rating={candidate.rating_score} />
      </div>

      {/* Matched skills chips */}
      {candidate.matching_skills.length > 0 && (
        <div className="flex flex-wrap gap-1.5 mb-3">
          {candidate.matching_skills.map((skill) => (
            <span
              key={skill}
              className="px-2 py-0.5 rounded-full bg-brand-50 text-brand-700 text-xs font-semibold border border-brand-100"
            >
              ✓ {skill}
            </span>
          ))}
        </div>
      )}

      {/* AI Justification */}
      <div className="bg-slate-50 border border-slate-100 rounded-xl p-3">
        <div className="flex items-center gap-1.5 mb-1.5">
          <Sparkles className="w-3.5 h-3.5 text-brand-500" />
          <span className="text-xs font-bold text-brand-600">AI Justification</span>
        </div>
        <p className="text-xs text-slate-600 leading-relaxed italic">
          &ldquo;{candidate.justification}&rdquo;
        </p>
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Reject Modal
// ─────────────────────────────────────────────────────────────────────────────
const RejectModal: React.FC<{
  onConfirm: (reason: string) => void;
  onClose: () => void;
}> = ({ onConfirm, onClose }) => {
  const [reason, setReason] = React.useState('');
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
      <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 animate-in fade-in zoom-in-95 duration-200">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-bold text-slate-900 text-lg">Reject AI Proposal</h3>
          <button onClick={onClose} className="p-1 rounded-lg hover:bg-slate-100 text-slate-500">
            <X className="w-5 h-5" />
          </button>
        </div>
        <p className="text-sm text-slate-600 mb-4">
          Provide a brief reason for rejection. The AI agent will log this as auditable feedback.
        </p>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="e.g., Headcount requirements changed, need more experienced candidates..."
          rows={3}
          className="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-red-400 focus:border-transparent"
        />
        <div className="flex gap-3 mt-4">
          <button
            onClick={onClose}
            className="flex-1 px-4 py-2.5 rounded-xl border border-slate-200 text-slate-700 text-sm font-semibold hover:bg-slate-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={() => onConfirm(reason || 'Rejected by organizer')}
            className="flex-1 px-4 py-2.5 rounded-xl bg-red-600 text-white text-sm font-semibold hover:bg-red-700 transition-colors"
          >
            Confirm Rejection
          </button>
        </div>
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// AI Staffing Hub Page
// ─────────────────────────────────────────────────────────────────────────────
export const AIMatchingPage: React.FC = () => {
  const {
    selectedRole,
    headcount,
    minExperienceLevel,
    requiredSkills,
    isMatching,
    currentExecutionStep,
    matchingResult,
    approvalStatus,
    error,
    successMessage,
    showObservabilityDrawer,
    setSelectedRole,
    setHeadcount,
    setMinExperienceLevel,
    toggleSkill,
    runMatching,
    approveProposal,
    rejectProposal,
    resetMatching,
    toggleObservabilityDrawer,
    clearMessages,
  } = useMatchingStore();

  const [showRejectModal, setShowRejectModal] = React.useState(false);

  // Auto-clear success toast after 6s
  useEffect(() => {
    if (successMessage) {
      const t = setTimeout(() => clearMessages(), 6000);
      return () => clearTimeout(t);
    }
  }, [successMessage, clearMessages]);

  const handleApprove = () => {
    approveProposal(matchingResult?.workflow_run_id);
  };

  const handleRejectConfirm = (reason: string) => {
    setShowRejectModal(false);
    rejectProposal(matchingResult?.workflow_run_id, reason);
  };

  const canRunMatching = !isMatching && selectedRole.trim().length > 0 && headcount >= 1;
  const showResults = matchingResult !== null && !isMatching;
  const isHITLVisible = showResults && approvalStatus === 'PendingApproval';

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* ── Page Header ── */}
      <div className="mb-8">
        <div className="flex items-center gap-3 mb-2">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-brand-700 to-indigo-500 flex items-center justify-center text-white shadow-md">
            <Cpu className="w-5 h-5" />
          </div>
          <div>
            <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">
              AI Staffing &amp; Matching Hub
            </h1>
            <p className="text-sm text-slate-500">
              Autonomous volunteer matching powered by the <strong>Volunteer Matching Agent</strong>{' '}
              (Section 9.1 · Human-in-the-Loop)
            </p>
          </div>
        </div>
      </div>

      {/* ── Architecture Badge ── */}
      <div className="flex items-center gap-2 px-3.5 py-2 rounded-xl bg-indigo-50 border border-indigo-100 text-xs text-indigo-700 font-medium mb-6 w-fit">
        <Info className="w-4 h-4 text-indigo-500" />
        Architecture: React → ASP.NET Core Gateway (
        <code className="font-mono">/api/agents/match-volunteers</code>) → Python AI Microservice
        (localhost:8000) — React never calls Python directly.
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* ════════════════════════════════════════════════════════
            COLUMN 1: Configuration Panel
            ════════════════════════════════════════════════════════ */}
        <div className="xl:col-span-1 space-y-4">
          {/* Role Selection Card */}
          <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm">
            <h2 className="font-bold text-slate-800 text-sm mb-4 flex items-center gap-2">
              <span className="w-6 h-6 rounded-md bg-brand-100 text-brand-700 text-xs font-bold flex items-center justify-center">1</span>
              Role Configuration
            </h2>

            {/* Role Name */}
            <div className="mb-4">
              <label className="block text-xs font-semibold text-slate-600 mb-1.5">
                Volunteer Role
              </label>
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                className="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white text-slate-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
              >
                {AVAILABLE_ROLES.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </div>

            {/* Headcount */}
            <div className="mb-4">
              <label className="block text-xs font-semibold text-slate-600 mb-1.5">
                Required Headcount
              </label>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setHeadcount(headcount - 1)}
                  disabled={headcount <= 1}
                  className="w-8 h-8 rounded-lg border border-slate-200 flex items-center justify-center text-slate-500 hover:bg-slate-50 disabled:opacity-40 font-bold text-lg"
                >
                  −
                </button>
                <span className="w-12 text-center font-bold text-slate-900 text-lg tabular-nums">
                  {headcount}
                </span>
                <button
                  onClick={() => setHeadcount(headcount + 1)}
                  className="w-8 h-8 rounded-lg border border-slate-200 flex items-center justify-center text-slate-500 hover:bg-slate-50 font-bold text-lg"
                >
                  +
                </button>
              </div>
            </div>

            {/* Minimum Experience */}
            <div className="mb-4">
              <label className="block text-xs font-semibold text-slate-600 mb-1.5">
                Min. Experience Tier
              </label>
              <div className="flex gap-1.5">
                {(['Beginner', 'Intermediate', 'Advanced'] as const).map((tier) => (
                  <button
                    key={tier}
                    onClick={() => setMinExperienceLevel(tier)}
                    className={`flex-1 py-1.5 rounded-lg text-xs font-semibold border transition-colors ${
                      minExperienceLevel === tier
                        ? 'bg-brand-600 text-white border-brand-600'
                        : 'bg-white text-slate-600 border-slate-200 hover:border-brand-300 hover:text-brand-700'
                    }`}
                  >
                    {tier}
                  </button>
                ))}
              </div>
            </div>

            {/* Required Skills */}
            <div>
              <label className="block text-xs font-semibold text-slate-600 mb-1.5">
                Required Skills ({requiredSkills.length} selected)
              </label>
              <div className="flex flex-wrap gap-1.5">
                {SKILL_OPTIONS.map((skill) => {
                  const active = requiredSkills.includes(skill);
                  return (
                    <button
                      key={skill}
                      onClick={() => toggleSkill(skill)}
                      className={`px-2.5 py-1 rounded-full text-xs font-semibold border transition-all ${
                        active
                          ? 'bg-brand-600 text-white border-brand-600 shadow-sm'
                          : 'bg-white text-slate-600 border-slate-200 hover:border-brand-400 hover:text-brand-700'
                      }`}
                    >
                      {active ? '✓ ' : ''}{skill}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Run Agent Button */}
          <button
            id="run-matching-btn"
            onClick={runMatching}
            disabled={!canRunMatching}
            className={`w-full py-3.5 rounded-2xl font-bold text-sm transition-all duration-200 flex items-center justify-center gap-2.5 shadow-lg ${
              isMatching
                ? 'bg-brand-500 text-white cursor-wait animate-pulse'
                : canRunMatching
                ? 'bg-gradient-to-r from-brand-600 to-indigo-600 hover:from-brand-700 hover:to-indigo-700 text-white hover:shadow-brand-500/40 active:scale-95'
                : 'bg-slate-200 text-slate-400 cursor-not-allowed shadow-none'
            }`}
          >
            {isMatching ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Bot className="w-4 h-4" />
            )}
            {isMatching ? 'Running AI Matcher...' : 'Run Volunteer Matching Agent'}
          </button>

          {/* Reset button when results are showing */}
          {showResults && (
            <button
              onClick={resetMatching}
              className="w-full py-2.5 rounded-2xl font-semibold text-sm border border-slate-200 text-slate-600 hover:bg-slate-50 flex items-center justify-center gap-2 transition-colors"
            >
              <RotateCcw className="w-4 h-4" />
              Reset &amp; Re-configure
            </button>
          )}
        </div>

        {/* ════════════════════════════════════════════════════════
            COLUMN 2 + 3: Results & HITL Panel
            ════════════════════════════════════════════════════════ */}
        <div className="xl:col-span-2 space-y-4">

          {/* ── Agent Execution Progress ── */}
          {isMatching && (
            <div className="bg-white border border-brand-100 rounded-2xl p-5 shadow-sm">
              <h2 className="font-bold text-slate-800 text-sm mb-4 flex items-center gap-2">
                <Loader2 className="w-4 h-4 text-brand-500 animate-spin" />
                Agent Executing...
              </h2>
              <div className="space-y-2.5">
                {AGENT_EXECUTION_STEPS.map((step, idx) => (
                  <div key={step} className="flex items-center gap-3">
                    <div
                      className={`w-5 h-5 rounded-full border-2 flex items-center justify-center flex-shrink-0 transition-all duration-300 ${
                        idx < currentExecutionStep
                          ? 'border-emerald-500 bg-emerald-500'
                          : idx === currentExecutionStep
                          ? 'border-brand-500 bg-brand-100 animate-pulse'
                          : 'border-slate-200 bg-white'
                      }`}
                    >
                      {idx < currentExecutionStep && (
                        <CheckCircle2 className="w-3 h-3 text-white" />
                      )}
                    </div>
                    <span
                      className={`text-sm transition-colors duration-300 ${
                        idx <= currentExecutionStep ? 'text-slate-800 font-medium' : 'text-slate-400'
                      }`}
                    >
                      {step}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* ── Error banner ── */}
          {error && (
            <div className="flex items-start gap-3 px-4 py-3.5 bg-red-50 border border-red-200 rounded-xl text-red-700 text-sm">
              <AlertTriangle className="w-4 h-4 mt-0.5 flex-shrink-0 text-red-500" />
              <div>
                <p className="font-bold">Matching Error</p>
                <p className="text-red-600 mt-0.5">{error}</p>
              </div>
            </div>
          )}

          {/* ── Success banner ── */}
          {successMessage && (
            <div
              id="approval-success-banner"
              className={`flex items-start gap-3 px-4 py-3.5 rounded-xl text-sm border animate-in fade-in slide-in-from-top-2 duration-300 ${
                approvalStatus === 'Approved'
                  ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
                  : 'bg-amber-50 border-amber-200 text-amber-800'
              }`}
            >
              {approvalStatus === 'Approved' ? (
                <ShieldCheck className="w-4 h-4 mt-0.5 flex-shrink-0" />
              ) : (
                <XCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              )}
              <p className="font-semibold">{successMessage}</p>
            </div>
          )}

          {/* ── Workflow Observability Banner ── */}
          {showResults && matchingResult && (
            <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm">
              <div className="flex flex-wrap items-center gap-3 mb-3">
                <div className="flex items-center gap-2 text-xs text-slate-500">
                  <Hash className="w-3.5 h-3.5" />
                  <span className="font-mono text-slate-700 font-semibold text-[11px] truncate max-w-[200px]">
                    {matchingResult.workflow_id}
                  </span>
                </div>
                <div className="flex items-center gap-1.5 text-xs text-slate-500">
                  <Clock className="w-3.5 h-3.5" />
                  <span className="font-semibold text-slate-700">
                    {matchingResult.execution_time_ms}ms
                  </span>
                </div>
                <StatusBadge status={matchingResult.status} />
                {matchingResult.is_fallback && (
                  <span className="px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 text-xs font-semibold border border-amber-200">
                    ⚠ Fallback Mode (AI offline)
                  </span>
                )}
              </div>

              {/* Unfulfilled slots alert */}
              {matchingResult.unfulfilled_slots > 0 && (
                <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-amber-50 border border-amber-200 text-xs text-amber-700 font-medium">
                  <AlertCircle className="w-3.5 h-3.5 flex-shrink-0" />
                  {matchingResult.status === 'SAFE_FAILURE'
                    ? `Safe Failure: No qualified candidates found. ${matchingResult.unfulfilled_slots} slot(s) remain unfilled. The agent refused to hallucinate candidates.`
                    : `Partial Match: ${matchingResult.unfulfilled_slots} of ${matchingResult.headcount_needed} slot(s) unfilled. Consider adjusting required skills or experience tier.`}
                </div>
              )}

              {/* Observability Drawer Toggle */}
              <button
                onClick={toggleObservabilityDrawer}
                className="mt-3 flex items-center gap-1.5 text-xs text-brand-600 font-semibold hover:underline"
              >
                <Zap className="w-3.5 h-3.5" />
                {showObservabilityDrawer ? 'Hide' : 'Show'} Tool Execution Trace
                {showObservabilityDrawer ? (
                  <ChevronUp className="w-3.5 h-3.5" />
                ) : (
                  <ChevronDown className="w-3.5 h-3.5" />
                )}
              </button>

              {/* Collapsible Observability Drawer */}
              {showObservabilityDrawer && (
                <div className="mt-3 border-t border-slate-100 pt-3 space-y-2">
                  <p className="text-xs font-bold text-slate-600 mb-2">Allow-Listed Tool Trace:</p>
                  {[
                    {
                      name: 'fetch_eligible_applicants',
                      params: `{ event_id: "${matchingResult.workflow_id.slice(0, 8)}..." }`,
                      duration: `${Math.round(matchingResult.execution_time_ms * 0.3)}ms`,
                    },
                    {
                      name: 'compute_skill_affinity_score',
                      params: `{ required_skills: [${matchingResult.matched_candidates[0]?.matching_skills.map((s) => `"${s}"`).join(', ') || '...'}] }`,
                      duration: `${Math.round(matchingResult.execution_time_ms * 0.5)}ms`,
                    },
                    {
                      name: 'log_agent_observability',
                      params: `{ workflow_id: "${matchingResult.workflow_id.slice(0, 8)}...", tool_name: "workflow_completion" }`,
                      duration: `${Math.round(matchingResult.execution_time_ms * 0.2)}ms`,
                    },
                  ].map((tool) => (
                    <div
                      key={tool.name}
                      className="bg-slate-50 rounded-lg px-3 py-2 font-mono text-[11px] text-slate-700 space-y-0.5"
                    >
                      <div className="flex items-center justify-between">
                        <span className="text-brand-700 font-bold">{tool.name}()</span>
                        <span className="text-slate-400">{tool.duration}</span>
                      </div>
                      <div className="text-slate-500">{tool.params}</div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* ── Candidate Result Cards ── */}
          {showResults && matchingResult && matchingResult.matched_candidates.length > 0 && (
            <div>
              <h2 className="font-bold text-slate-800 text-sm mb-3 flex items-center gap-2">
                <span className="w-6 h-6 rounded-md bg-emerald-100 text-emerald-700 text-xs font-bold flex items-center justify-center">
                  ✓
                </span>
                Ranked Candidates — {matchingResult.matched_candidates.length} Qualified for &quot;{matchingResult.role_name}&quot;
              </h2>
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {matchingResult.matched_candidates.map((candidate, idx) => (
                  <CandidateCard
                    key={candidate.volunteer_id}
                    candidate={candidate}
                    rank={idx + 1}
                  />
                ))}
              </div>
            </div>
          )}

          {/* ── Safe Failure Empty State ── */}
          {showResults &&
            matchingResult &&
            matchingResult.matched_candidates.length === 0 && (
              <div className="bg-white border border-red-100 rounded-2xl p-8 text-center shadow-sm">
                <div className="w-12 h-12 rounded-full bg-red-50 flex items-center justify-center mx-auto mb-4">
                  <AlertTriangle className="w-6 h-6 text-red-400" />
                </div>
                <h3 className="font-bold text-slate-800 mb-1">Safe Failure — No Candidates Matched</h3>
                <p className="text-sm text-slate-500 max-w-sm mx-auto">
                  The AI agent enforced Guardrail 3: no qualified candidates were found for this role and skill
                  combination. Zero fake volunteers were generated. Adjust criteria and re-run.
                </p>
              </div>
            )}

          {/* ── Human-in-the-Loop HITL Action Bar ── */}
          {isHITLVisible && (
            <div className="bg-white border-2 border-brand-200 rounded-2xl p-5 shadow-sm">
              <div className="flex items-center gap-2 mb-3">
                <ShieldCheck className="w-5 h-5 text-brand-600" />
                <h2 className="font-bold text-slate-900">Human-in-the-Loop Review (Section 9.1)</h2>
              </div>
              <p className="text-sm text-slate-600 mb-4">
                Review the AI-ranked candidate proposal above. Your decision will be persisted in the
                database via the ASP.NET Core backend. The AI agent cannot auto-approve — organizer
                action is mandatory.
              </p>
              <div className="flex gap-3">
                <button
                  id="approve-roster-btn"
                  onClick={handleApprove}
                  className="flex-1 py-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-sm flex items-center justify-center gap-2 transition-colors shadow-md shadow-emerald-500/20 active:scale-95"
                >
                  <CheckCircle2 className="w-4 h-4" />
                  Approve &amp; Confirm Roster
                </button>
                <button
                  id="reject-proposal-btn"
                  onClick={() => setShowRejectModal(true)}
                  className="flex-1 py-3 rounded-xl border-2 border-red-200 text-red-600 hover:bg-red-50 font-bold text-sm flex items-center justify-center gap-2 transition-colors active:scale-95"
                >
                  <XCircle className="w-4 h-4" />
                  Reject Proposal
                </button>
              </div>
            </div>
          )}

          {/* ── Idle state ── */}
          {!isMatching && !matchingResult && !error && (
            <div className="bg-white border border-dashed border-slate-200 rounded-2xl p-12 text-center">
              <div className="w-16 h-16 rounded-2xl bg-gradient-to-tr from-brand-100 to-indigo-100 flex items-center justify-center mx-auto mb-4">
                <Bot className="w-8 h-8 text-brand-500" />
              </div>
              <h3 className="font-bold text-slate-700 mb-1">Volunteer Matching Agent Ready</h3>
              <p className="text-sm text-slate-400 max-w-xs mx-auto">
                Configure the role criteria on the left and click{' '}
                <strong>&quot;Run Volunteer Matching Agent&quot;</strong> to start the AI matching workflow.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Reject Modal */}
      {showRejectModal && (
        <RejectModal
          onConfirm={handleRejectConfirm}
          onClose={() => setShowRejectModal(false)}
        />
      )}
    </div>
  );
};
