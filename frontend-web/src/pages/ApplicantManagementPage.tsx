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
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Toast Notification (Top Right Floating) */}
      {successMessage && (
        <div className="fixed top-20 right-6 z-50 flex items-center space-x-3 bg-emerald-900 text-white px-5 py-3.5 rounded-2xl shadow-xl border border-emerald-700 animate-in slide-in-from-top-4 duration-300">
          <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
          <div className="text-sm font-semibold">{successMessage}</div>
          <button
            onClick={clearMessages}
            className="text-emerald-300 hover:text-white transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <div className="inline-flex items-center space-x-2 px-3 py-1 rounded-full bg-brand-50 border border-brand-200 text-brand-700 text-xs font-bold mb-2">
            <Sparkles className="w-3.5 h-3.5" />
            <span>Organizer Console • SE3090 Module 2</span>
          </div>
          <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">
            Volunteer Applications
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Review incoming candidate applications, assess qualifications, and advance volunteers through recruitment workflows.
          </p>
        </div>

        <div className="flex items-center space-x-3">
          <button
            onClick={loadDemoData}
            className="px-4 py-2.5 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 text-sm font-semibold rounded-xl shadow-sm transition-all flex items-center space-x-2"
            title="Load sample applicants"
          >
            <RotateCcw className="w-4 h-4 text-slate-500" />
            <span>Reset Demo Data</span>
          </button>
        </div>
      </div>

      {/* KPI Metric Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center space-x-4">
          <div className="w-12 h-12 rounded-xl bg-brand-50 border border-brand-100 flex items-center justify-center text-brand-600">
            <Users className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-slate-900">{totalCount}</div>
            <div className="text-xs font-medium text-slate-500">Total Applicants</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center space-x-4">
          <div className="w-12 h-12 rounded-xl bg-blue-50 border border-blue-100 flex items-center justify-center text-blue-600">
            <Clock className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-slate-900">{underReviewCount}</div>
            <div className="text-xs font-medium text-slate-500">Pending Review</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center space-x-4">
          <div className="w-12 h-12 rounded-xl bg-purple-50 border border-purple-100 flex items-center justify-center text-purple-600">
            <Sparkles className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-slate-900">{shortlistedCount}</div>
            <div className="text-xs font-medium text-slate-500">Shortlisted Pool</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center space-x-4">
          <div className="w-12 h-12 rounded-xl bg-emerald-50 border border-emerald-100 flex items-center justify-center text-emerald-600">
            <CheckCheck className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-slate-900">{acceptedCount}</div>
            <div className="text-xs font-medium text-slate-500">Accepted Crew</div>
          </div>
        </div>
      </div>

      {/* Error Banner with Retry */}
      {error && (
        <div className="mb-6 p-4 bg-rose-50 border border-rose-200 rounded-2xl flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-rose-800 text-sm">
          <div className="flex items-center space-x-3">
            <AlertTriangle className="w-5 h-5 text-rose-600 shrink-0" />
            <div>
              <div className="font-bold">Backend Connection Notice</div>
              <div className="text-xs text-rose-700">{error}</div>
            </div>
          </div>
          <div className="flex items-center space-x-2 shrink-0">
            <button
              onClick={() => fetchApplicants(currentEventId)}
              className="px-3 py-1.5 bg-rose-600 hover:bg-rose-700 text-white rounded-lg text-xs font-bold transition-colors"
            >
              Retry Connection
            </button>
            <button
              onClick={loadDemoData}
              className="px-3 py-1.5 bg-white border border-rose-300 text-rose-700 hover:bg-rose-100/50 rounded-lg text-xs font-bold transition-colors"
            >
              Use Demo View
            </button>
            <button onClick={clearMessages} className="p-1 text-rose-500 hover:text-rose-800">
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Search and Sort Toolbar */}
      <SearchAndSortBar />

      {/* Status Filter Tabs */}
      <div className="mb-4">
        <StatusFilterTabs />
      </div>

      {/* Main Content Area */}
      {isLoading && applicants.length === 0 ? (
        /* Loading Skeleton */
        <div className="bg-white rounded-2xl border border-slate-200 p-8 shadow-sm space-y-4">
          <div className="h-6 bg-slate-100 rounded w-1/4 animate-pulse"></div>
          <div className="space-y-3">
            {[1, 2, 3, 4, 5].map((i) => (
              <div key={i} className="h-14 bg-slate-50 rounded-xl animate-pulse flex items-center px-4 space-x-4">
                <div className="w-10 h-10 bg-slate-200 rounded-full"></div>
                <div className="flex-1 space-y-2">
                  <div className="h-4 bg-slate-200 rounded w-1/3"></div>
                  <div className="h-3 bg-slate-200 rounded w-1/4"></div>
                </div>
                <div className="h-8 bg-slate-200 rounded w-24"></div>
              </div>
            ))}
          </div>
        </div>
      ) : filteredApplicants.length === 0 ? (
        /* Friendly Empty State */
        <div className="bg-white rounded-2xl border border-slate-200/80 p-12 text-center shadow-sm">
          <div className="w-16 h-16 bg-slate-100 text-slate-400 rounded-3xl flex items-center justify-center mx-auto mb-4 border border-slate-200">
            <Inbox className="w-8 h-8" />
          </div>
          <h3 className="text-lg font-bold text-slate-800 mb-1">
            No applicants found
          </h3>
          <p className="text-sm text-slate-500 max-w-md mx-auto mb-6">
            {searchQuery
              ? `No volunteers matching query "${searchQuery}". Try clearing the search or switching status tabs.`
              : statusFilter !== 'All'
              ? `There are currently no applicants with status "${statusFilter}".`
              : 'There are currently no volunteer applications registered for this event ID.'}
          </p>
          <div className="flex items-center justify-center gap-3">
            {searchQuery && (
              <button
                onClick={() => useApplicationStore.getState().setSearchQuery('')}
                className="px-4 py-2 bg-brand-50 text-brand-700 hover:bg-brand-100 rounded-xl text-xs font-bold transition-colors"
              >
                Clear Search Filter
              </button>
            )}
            <button
              onClick={loadDemoData}
              className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-xs font-bold shadow-sm transition-all"
            >
              Load Demo Applicants
            </button>
          </div>
        </div>
      ) : (
        /* Applicant Table */
        <ApplicantTable applicants={filteredApplicants} />
      )}

      {/* Applicant Detail View Modal */}
      <ApplicantDetailModal />
    </div>
  );
};
