import React from 'react';
import { useApplicationStore } from '../../stores/applicationStore';
import { ArrowDownAZ, ArrowUpDown, Calendar, RefreshCw, Search, Star } from 'lucide-react';

export const SearchAndSortBar: React.FC = () => {
  const {
    searchQuery,
    setSearchQuery,
    statusFilter,
    setStatusFilter,
    sortBy,
    setSortBy,
    currentEventId,
    setCurrentEventId,
    fetchApplicants,
    isLoading,
  } = useApplicationStore();

  const handleRefresh = () => {
    fetchApplicants(currentEventId);
  };

  return (
    <div className="flex flex-col sm:flex-row gap-3 items-stretch sm:items-center justify-between mb-4">
      {/* Search Input */}
      <div className="relative flex-1">
        <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search by volunteer or skill..."
          className="w-full pl-9 pr-8 py-2 border border-slate-200 rounded-lg text-xs focus:outline-none focus:ring-1 focus:ring-[#0F382C] text-slate-800 placeholder-slate-400"
        />
        {searchQuery && (
          <button
            onClick={() => setSearchQuery('')}
            className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[11px] text-slate-400 hover:text-slate-600 font-semibold"
          >
            Clear
          </button>
        )}
      </div>

      <div className="flex items-center gap-2">
        {/* Status Dropdown */}
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as any)}
          className="px-3 py-2 border border-slate-200 rounded-lg text-xs font-medium text-slate-700 bg-white focus:outline-none focus:ring-1 focus:ring-[#0F382C] cursor-pointer"
        >
          <option value="All">All statuses</option>
          <option value="Submitted">Pending Organizer</option>
          <option value="UnderReview">Under Review</option>
          <option value="Shortlisted">Shortlisted</option>
          <option value="Accepted">Approved</option>
          <option value="Rejected">Rejected</option>
        </select>

        {/* Sort Dropdown */}
        <div className="relative flex items-center">
          <ArrowUpDown className="w-3.5 h-3.5 text-slate-400 absolute left-2.5 pointer-events-none" />
          <select
            id="sort-select"
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as any)}
            className="pl-8 pr-3 py-2 border border-slate-200 rounded-lg text-xs font-medium text-slate-700 bg-white focus:outline-none focus:ring-1 focus:ring-[#0F382C] cursor-pointer"
          >
            <option value="date-desc">Newest First</option>
            <option value="date-asc">Oldest First</option>
            <option value="rating-desc">Highest Rating</option>
            <option value="name-asc">Name (A-Z)</option>
          </select>
        </div>

        {/* Refresh button */}
        <button
          onClick={handleRefresh}
          disabled={isLoading}
          className="p-2 border border-slate-200 hover:bg-slate-50 text-slate-600 rounded-lg text-xs transition-colors disabled:opacity-60"
          title="Refresh applicants from API"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin text-[#0F382C]' : ''}`} />
        </button>
      </div>
    </div>
  );
};
