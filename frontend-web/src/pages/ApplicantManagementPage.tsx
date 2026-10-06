import React, { useEffect, useMemo } from 'react';
import { useApplicationStore } from '../stores/applicationStore';
import { StatusFilterTabs } from '../components/applications/StatusFilterTabs';
import { SearchAndSortBar } from '../components/applications/SearchAndSortBar';
import { ApplicantTable } from '../components/applications/ApplicantTable';
import { ApplicantDetailModal } from '../components/applications/ApplicantDetailModal';
import {
  AlertTriangle,
  CheckCircle2,
  Inbox,
  Sparkles,
  Users,
  CheckCheck,
  Clock,
  RotateCcw,
  X,
} from 'lucide-react';

export const ApplicantManagementPage: React.FC = () => {
  const {
    applicants,
    isLoading,
    error,
    successMessage,
    searchQuery,
    statusFilter,
    sortBy,
    currentEventId,
    fetchApplicants,
    clearMessages,
    loadDemoData,
  } = useApplicationStore();

  // Initial fetch on mount
  useEffect(() => {
    fetchApplicants(currentEventId);
  }, []);

  // Auto-clear success toast after 4s
  useEffect(() => {
    if (successMessage) {
      const timer = setTimeout(() => {
        clearMessages();
      }, 4500);
      return () => clearTimeout(timer);
    }
  }, [successMessage, clearMessages]);

  // Filter and sort applicants
  const filteredApplicants = useMemo(() => {
    let result = [...applicants];

    // Status filter
    if (statusFilter !== 'All') {
      result = result.filter((app) => app.status === statusFilter);
    }

    // Search query filter (search by volunteer name or skill name)
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      result = result.filter((app) => {
        const name = (app.volunteer?.fullName || '').toLowerCase();
        const email = (app.volunteer?.email || '').toLowerCase();
        const hasMatchingSkill = app.volunteer?.skills.some((sk) =>
          sk.name.toLowerCase().includes(q)
        );
        return name.includes(q) || email.includes(q) || hasMatchingSkill;
      });
    }

    // Sorting
    result.sort((a, b) => {
      if (sortBy === 'date-desc') {
        return new Date(b.appliedAt).getTime() - new Date(a.appliedAt).getTime();
      }
      if (sortBy === 'date-asc') {
        return new Date(a.appliedAt).getTime() - new Date(b.appliedAt).getTime();
      }
      if (sortBy === 'rating-desc') {
        const rA = a.volunteer?.ratingScore ?? 0;
        const rB = b.volunteer?.ratingScore ?? 0;
        return rB - rA;
      }
      if (sortBy === 'name-asc') {
        const nA = a.volunteer?.fullName ?? '';
        const nB = b.volunteer?.fullName ?? '';
        return nA.localeCompare(nB);
      }
      return 0;
    });

    return result;
  }, [applicants, statusFilter, searchQuery, sortBy]);

  // Metrics summary
  const totalCount = applicants.length;
  const underReviewCount = applicants.filter(
    (a) => a.status === 'Submitted' || a.status === 'UnderReview'
  ).length;
  const shortlistedCount = applicants.filter((a) => a.status === 'Shortlisted').length;
  const acceptedCount = applicants.filter((a) => a.status === 'Accepted').length;

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      {/* Toast Notification */}
      {successMessage && (
        <div className="fixed top-16 right-6 z-50 flex items-center space-x-3 bg-[#0F382C] text-white px-5 py-3 rounded-xl shadow-xl border border-mint/20 animate-in slide-in-from-top-4 duration-300">
          <CheckCircle2 className="w-4 h-4 text-mint shrink-0" />
          <div className="text-xs font-semibold">{successMessage}</div>
          <button
            onClick={clearMessages}
            className="text-mint/70 hover:text-white transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="text-[11px] font-bold tracking-wider uppercase text-[#6B8E81] mb-1">
            Volunteer Management
          </div>
          <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
            Volunteer Applicants
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Manage volunteer applications, candidate qualifications, and organizer approvals.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={loadDemoData}
            className="px-3.5 py-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 text-xs font-semibold rounded-lg shadow-sm transition-all flex items-center gap-1.5"
            title="Load sample applicants"
          >
            <RotateCcw className="w-3.5 h-3.5 text-slate-500" />
            <span>Reset Demo Data</span>
          </button>
        </div>
      </div>

      {/* KPI Metric Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
          <div className="w-10 h-10 rounded-lg bg-[#E0F2FE] text-[#0284C7] flex items-center justify-center shrink-0">
            <Users className="w-5 h-5" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Total applicants</div>
            <div className="text-2xl font-bold text-slate-900">{totalCount}</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
          <div className="w-10 h-10 rounded-lg bg-[#FEF3C7] text-[#D97706] flex items-center justify-center shrink-0">
            <Clock className="w-5 h-5" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Pending organizer</div>
            <div className="text-2xl font-bold text-slate-900">{underReviewCount}</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
          <div className="w-10 h-10 rounded-lg bg-[#DCFCE7] text-[#16A34A] flex items-center justify-center shrink-0">
            <CheckCheck className="w-5 h-5" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Approved applicants</div>
            <div className="text-2xl font-bold text-slate-900">{acceptedCount}</div>
          </div>
        </div>
      </div>

      {/* Error Banner with Retry */}
      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 rounded-xl flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-rose-800 text-xs">
          <div className="flex items-center space-x-3">
            <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0" />
            <div>
              <div className="font-bold">Backend Connection Notice</div>
              <div className="text-rose-700">{error}</div>
            </div>
          </div>
          <div className="flex items-center space-x-2 shrink-0">
            <button
              onClick={() => fetchApplicants(currentEventId)}
              className="px-3 py-1 bg-rose-600 hover:bg-rose-700 text-white rounded-lg text-xs font-bold transition-colors"
            >
              Retry
            </button>
            <button
              onClick={loadDemoData}
              className="px-3 py-1 bg-white border border-rose-300 text-rose-700 hover:bg-rose-100/50 rounded-lg text-xs font-bold transition-colors"
            >
              Use Demo
            </button>
            <button onClick={clearMessages} className="p-1 text-rose-500 hover:text-rose-800">
              <X className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}

      {/* Main Table Card */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-sm p-6">
        <div className="mb-5 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold text-slate-900">Applicant Queue</h2>
              <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-slate-100 text-slate-600">
                {filteredApplicants.length}
              </span>
            </div>
            <p className="text-xs text-slate-500 mt-0.5">
              Review incoming volunteer applications, filter by status, and assign roles.
            </p>
          </div>
        </div>

        {/* Search & Status Filter Toolbar */}
        <SearchAndSortBar />

        {/* Table Content */}
        {isLoading && applicants.length === 0 ? (
          <div className="p-8 space-y-4">
            <div className="h-5 bg-slate-100 rounded w-1/4 animate-pulse"></div>
            <div className="space-y-3">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="h-12 bg-slate-50 rounded-xl animate-pulse"></div>
              ))}
            </div>
          </div>
        ) : filteredApplicants.length === 0 ? (
          <div className="p-12 text-center">
            <div className="w-12 h-12 bg-slate-100 text-slate-400 rounded-2xl flex items-center justify-center mx-auto mb-3 border border-slate-200">
              <Inbox className="w-6 h-6" />
            </div>
            <h3 className="text-sm font-bold text-slate-800 mb-1">
              No applicants found
            </h3>
            <p className="text-xs text-slate-500 max-w-md mx-auto mb-4">
              {searchQuery
                ? `No volunteers matching query "${searchQuery}".`
                : statusFilter !== 'All'
                ? `There are currently no applicants with status "${statusFilter}".`
                : 'There are currently no volunteer applications registered for this event ID.'}
            </p>
            <button
              onClick={loadDemoData}
              className="px-3 py-1.5 bg-[#0F382C] hover:bg-[#163D30] text-white rounded-lg text-xs font-semibold shadow-sm transition-all"
            >
              Load Demo Applicants
            </button>
          </div>
        ) : (
          <ApplicantTable applicants={filteredApplicants} />
        )}
      </div>

      {/* Applicant Detail View Modal */}
      <ApplicantDetailModal />
    </div>
  );
};
