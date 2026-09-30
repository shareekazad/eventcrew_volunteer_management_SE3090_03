import React from 'react';
import { useApplicationStore } from '../../stores/applicationStore';
import { ArrowDownAZ, ArrowUpDown, Calendar, RefreshCw, Search, Star } from 'lucide-react';

export const SearchAndSortBar: React.FC = () => {
  const {
    searchQuery,
    setSearchQuery,
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
    <div className="bg-white rounded-2xl border border-slate-200/80 p-4 shadow-sm mb-6 flex flex-col md:flex-row gap-3 items-stretch md:items-center justify-between">
      {/* Search Input */}
      <div className="relative flex-1">
        <Search className="w-5 h-5 absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search applicants by volunteer name or skills..."
          className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:bg-white transition-all text-slate-800 placeholder-slate-400"
        />
        {searchQuery && (
          <button
            onClick={() => setSearchQuery('')}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-slate-400 hover:text-slate-600 font-semibold"
          >
            Clear
          </button>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-2.5">
        {/* Sort Dropdown */}
        <div className="relative flex items-center">
          <label htmlFor="sort-select" className="sr-only">Sort applicants</label>
          <ArrowUpDown className="w-4 h-4 text-slate-400 absolute left-3 pointer-events-none" />
          <select
            id="sort-select"
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as any)}
            className="pl-9 pr-8 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm font-medium text-slate-700 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-brand-500 cursor-pointer transition-all"
          >
            <option value="date-desc">Newest Applied First</option>
            <option value="date-asc">Oldest Applied First</option>
            <option value="rating-desc">Highest Rating Score</option>
            <option value="name-asc">Volunteer Name (A-Z)</option>
          </select>
        </div>

        {/* Event ID quick switch / reload */}
        <div className="flex items-center gap-2">
          <input
            type="text"
            value={currentEventId}
            onChange={(e) => setCurrentEventId(e.target.value)}
            placeholder="Event GUID..."
            title="Event UUID"
            className="hidden lg:block w-36 px-2.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono text-slate-600 truncate focus:outline-none focus:ring-2 focus:ring-brand-500"
          />
          <button
            onClick={handleRefresh}
            disabled={isLoading}
            className="flex items-center space-x-1.5 px-3.5 py-2.5 bg-slate-100 hover:bg-slate-200 active:bg-slate-300 text-slate-700 text-sm font-medium rounded-xl transition-colors disabled:opacity-60"
            title="Fetch from API"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin text-brand-600' : ''}`} />
            <span className="hidden sm:inline">Refresh</span>
          </button>
        </div>
      </div>
    </div>
  );
};
