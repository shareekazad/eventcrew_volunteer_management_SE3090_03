import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { Layers, Radio, Shield, Menu, X } from 'lucide-react';
import { getDevOrganizerToken } from '../services/api';
import { useApplicationStore } from '../stores/applicationStore';

export const Navbar: React.FC = () => {
  const [tokenCopied, setTokenCopied] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const fetchApplicants = useApplicationStore((state) => state.fetchApplicants);

  const copyToken = () => {
    const token = getDevOrganizerToken();
    localStorage.setItem('token', token);
    navigator.clipboard.writeText(token);
    setTokenCopied(true);
    setTimeout(() => setTokenCopied(false), 2000);
    fetchApplicants();
  };

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    `flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-bold transition-all duration-200 ${
      isActive
        ? 'bg-brand-600 text-white shadow-md shadow-brand-500/30 ring-2 ring-brand-400/20'
        : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
    }`;

  return (
    <header className="bg-white border-b border-slate-200/80 sticky top-0 z-40 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        {/* Brand Logo & Event Selector */}
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-brand-700 to-indigo-500 flex items-center justify-center text-white shadow-md shadow-brand-500/20">
            <Layers className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <span className="font-extrabold text-slate-900 tracking-tight text-lg">EventCrew</span>
              <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-slate-100 text-slate-600 border border-slate-200">
                Organizer Hub
              </span>
            </div>
            <p className="text-[11px] text-slate-400 hidden sm:block">
              Volunteer Lifecycle &amp; AI Staffing Management
            </p>
          </div>

          {/* Event Selector Pill */}
          <div className="hidden lg:flex items-center gap-1.5 ml-4 px-3 py-1.5 rounded-xl bg-amber-50 border border-amber-200/80 text-xs font-bold text-amber-900 shadow-sm">
            <span>🎪 TechFest 2026</span>
          </div>
        </div>

        {/* Desktop Navigation */}
        <nav className="hidden md:flex items-center gap-2">
          <NavLink to="/applicants" className={navLinkClass}>
            <span>📋 Applicant Queue</span>
          </NavLink>
          <NavLink to="/ai-matching" className={navLinkClass}>
            <span className="flex items-center gap-1.5">
              <span>✨ AI Staffing Agent</span>
              <span className="px-1.5 py-0.5 text-[10px] uppercase font-extrabold tracking-wider rounded-md bg-gradient-to-r from-purple-600 to-indigo-600 text-white shadow-sm">
                AI
              </span>
            </span>
          </NavLink>
        </nav>

        {/* Right-side badges */}
        <div className="hidden md:flex items-center space-x-3">
          <div className="flex items-center space-x-2 px-3 py-1.5 rounded-xl bg-slate-100/80 border border-slate-200 text-xs text-slate-600">
            <Radio className="w-3.5 h-3.5 text-emerald-500 animate-pulse" />
            <span>Backend:</span>
            <span className="font-mono text-slate-800 font-semibold">localhost:5100</span>
          </div>

          <button
            onClick={copyToken}
            className="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-medium transition-colors"
            title="Copy active Organizer Bearer JWT token"
          >
            <Shield className="w-3.5 h-3.5 text-brand-600" />
            <span>{tokenCopied ? '✓ JWT Copied!' : 'Copy JWT'}</span>
          </button>

          <div className="flex items-center space-x-2.5 pl-2 border-l border-slate-200">
            <div className="w-8 h-8 rounded-full bg-brand-600 text-white font-bold text-xs flex items-center justify-center">
              LO
            </div>
            <div className="text-left">
              <div className="text-xs font-bold text-slate-800">Lead Organizer</div>
              <div className="text-[10px] text-emerald-600 font-semibold flex items-center">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 mr-1 inline-block" />
                Organizer Role
              </div>
            </div>
          </div>
        </div>

        {/* Mobile hamburger */}
        <button
          className="md:hidden p-2 rounded-lg text-slate-600 hover:bg-slate-100"
          onClick={() => setMobileOpen(!mobileOpen)}
        >
          {mobileOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
        </button>
      </div>

      {/* Mobile Drawer */}
      {mobileOpen && (
        <div className="md:hidden border-t border-slate-200 bg-white px-4 py-3 space-y-2 shadow-lg">
          <div className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-amber-50 border border-amber-200/80 text-xs font-bold text-amber-900 w-fit mb-2">
            <span>🎪 TechFest 2026</span>
          </div>
          <NavLink
            to="/applicants"
            className={navLinkClass}
            onClick={() => setMobileOpen(false)}
          >
            <span>📋 Applicant Queue</span>
          </NavLink>
          <NavLink
            to="/ai-matching"
            className={navLinkClass}
            onClick={() => setMobileOpen(false)}
          >
            <span className="flex items-center gap-1.5">
              <span>✨ AI Staffing Agent</span>
              <span className="px-1.5 py-0.5 text-[10px] uppercase font-extrabold tracking-wider rounded-md bg-gradient-to-r from-purple-600 to-indigo-600 text-white shadow-sm">
                AI
              </span>
            </span>
          </NavLink>
          <div className="pt-2 mt-2 border-t border-slate-100 flex items-center gap-2 text-xs text-slate-500">
            <Radio className="w-3.5 h-3.5 text-emerald-500 animate-pulse" />
            <span>Backend: localhost:5100</span>
          </div>
        </div>
      )}
    </header>
  );
};
