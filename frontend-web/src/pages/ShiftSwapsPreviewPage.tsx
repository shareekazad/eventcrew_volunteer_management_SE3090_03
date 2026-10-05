import React, { useState } from 'react';
import { ArrowLeftRight, Clock, Check, Search } from 'lucide-react';

interface SwapItem {
  id: string;
  requesterName: string;
  requesterEmail: string;
  sourceShift: string;
  sourceEvent: string;
  targetVolunteerName: string;
  targetVolunteerEmail: string;
  targetShift: string;
  targetEvent: string;
  reason: string;
  status: 'Pending Organizer' | 'Approved' | 'Rejected';
}

const mockSwaps: SwapItem[] = [
  {
    id: '1',
    requesterName: 'Alex Morgan',
    requesterEmail: 'alex@example.test',
    sourceShift: 'Registration Desk',
    sourceEvent: 'Community Arts Festival',
    targetVolunteerName: 'Taylor Kim',
    targetVolunteerEmail: 'taylor@example.test',
    targetShift: 'Main Stage Support',
    targetEvent: 'Community Arts Festival',
    reason: 'Schedule conflict',
    status: 'Pending Organizer',
  },
  {
    id: '2',
    requesterName: 'Jamie Lee',
    requesterEmail: 'jamie@example.test',
    sourceShift: 'Registration Desk',
    sourceEvent: 'Community Arts Festival',
    targetVolunteerName: 'Riley Patel',
    targetVolunteerEmail: 'riley@example.test',
    targetShift: 'Guest Support',
    targetEvent: 'Community Arts Festival',
    reason: 'Would like to help with guest support',
    status: 'Approved',
  },
  {
    id: '3',
    requesterName: 'Alex Morgan',
    requesterEmail: 'alex@example.test',
    sourceShift: 'Main Stage Support',
    sourceEvent: 'Community Arts Festival',
    targetVolunteerName: 'Jamie Lee',
    targetVolunteerEmail: 'jamie@example.test',
    targetShift: 'Registration Desk',
    targetEvent: 'Community Arts Festival',
    reason: 'Example rejected request',
    status: 'Rejected',
  },
];

export const ShiftSwapsPreviewPage: React.FC = () => {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');

  const filtered = mockSwaps.filter((item) => {
    const matchesSearch =
      item.requesterName.toLowerCase().includes(search.toLowerCase()) ||
      item.targetVolunteerName.toLowerCase().includes(search.toLowerCase()) ||
      item.sourceShift.toLowerCase().includes(search.toLowerCase());
    const matchesStatus =
      statusFilter === 'All' || item.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      {/* Page Header */}
      <div>
        <div className="text-[11px] font-bold tracking-wider uppercase text-[#6B8E81] mb-1">
          Rostering &amp; Swaps
        </div>
        <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
          Shift Swaps
        </h1>
        <p className="text-sm text-slate-500 mt-1">
          Manage volunteer shift swap requests and organizer approvals.
        </p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
          <div className="w-10 h-10 rounded-lg bg-[#E0F2FE] text-[#0284C7] flex items-center justify-center shrink-0">
            <ArrowLeftRight className="w-5 h-5" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Total requests</div>
            <div className="text-2xl font-bold text-slate-900">3</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
          <div className="w-10 h-10 rounded-lg bg-[#FEF3C7] text-[#D97706] flex items-center justify-center shrink-0">
            <Clock className="w-5 h-5" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Pending organizer</div>
            <div className="text-2xl font-bold text-slate-900">1</div>
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
          <div className="w-10 h-10 rounded-lg bg-[#DCFCE7] text-[#16A34A] flex items-center justify-center shrink-0">
            <Check className="w-5 h-5" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Approved swaps</div>
            <div className="text-2xl font-bold text-slate-900">1</div>
          </div>
        </div>
      </div>

      {/* Main Table Card */}
      <div className="bg-white rounded-2xl border border-slate-200/80 shadow-sm p-6">
        <div className="mb-5">
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-bold text-slate-900">Swap Requests</h2>
            <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-slate-100 text-slate-600">
              {filtered.length}
            </span>
          </div>
          <p className="text-xs text-slate-500 mt-0.5">
            Review incoming and outgoing shift swap requests.
          </p>
        </div>

        {/* Search & Filter Toolbar */}
        <div className="flex flex-col sm:flex-row gap-3 mb-6">
          <div className="relative flex-1">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by volunteer or shift title..."
              className="w-full pl-9 pr-4 py-2 border border-slate-200 rounded-lg text-xs focus:outline-none focus:ring-1 focus:ring-[#0F382C] text-slate-800 placeholder-slate-400"
            />
          </div>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="px-3 py-2 border border-slate-200 rounded-lg text-xs font-medium text-slate-700 bg-white focus:outline-none focus:ring-1 focus:ring-[#0F382C] cursor-pointer"
          >
            <option value="All">All statuses</option>
            <option value="Pending Organizer">Pending Organizer</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
          </select>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="border-b border-slate-100 text-slate-400 font-medium">
                <th className="py-3 px-3">Requester</th>
                <th className="py-3 px-3">Source Shift</th>
                <th className="py-3 px-3">Target Volunteer</th>
                <th className="py-3 px-3">Target Shift</th>
                <th className="py-3 px-3">Reason</th>
                <th className="py-3 px-3">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100/80">
              {filtered.map((item) => (
                <tr key={item.id} className="hover:bg-slate-50/50 transition-colors">
                  <td className="py-3.5 px-3">
                    <div className="font-semibold text-slate-900">{item.requesterName}</div>
                    <div className="text-[11px] text-slate-400">{item.requesterEmail}</div>
                  </td>
                  <td className="py-3.5 px-3">
                    <div className="font-medium text-slate-800">{item.sourceShift}</div>
                    <div className="text-[11px] text-slate-400">{item.sourceEvent}</div>
                  </td>
                  <td className="py-3.5 px-3">
                    <div className="font-semibold text-slate-900">{item.targetVolunteerName}</div>
                    <div className="text-[11px] text-slate-400">{item.targetVolunteerEmail}</div>
                  </td>
                  <td className="py-3.5 px-3">
                    <div className="font-medium text-slate-800">{item.targetShift}</div>
                    <div className="text-[11px] text-slate-400">{item.targetEvent}</div>
                  </td>
                  <td className="py-3.5 px-3 text-slate-600 max-w-[200px] truncate">
                    {item.reason}
                  </td>
                  <td className="py-3.5 px-3">
                    {item.status === 'Pending Organizer' && (
                      <span className="inline-flex items-center text-xs font-medium text-slate-700">
                        <span className="w-2 h-2 rounded-full bg-amber-500 mr-2 shrink-0"></span>
                        Pending Organizer
                      </span>
                    )}
                    {item.status === 'Approved' && (
                      <span className="inline-flex items-center text-xs font-medium text-slate-700">
                        <span className="w-2 h-2 rounded-full bg-emerald-500 mr-2 shrink-0"></span>
                        Approved
                      </span>
                    )}
                    {item.status === 'Rejected' && (
                      <span className="inline-flex items-center text-xs font-medium text-slate-700">
                        <span className="w-2 h-2 rounded-full bg-rose-500 mr-2 shrink-0"></span>
                        Rejected
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
