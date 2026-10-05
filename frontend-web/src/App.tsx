import React, { useState } from 'react';
import { BrowserRouter, Routes, Route, Link, useLocation } from 'react-router-dom';
import { ApplicantManagementPage } from './pages/ApplicantManagementPage';
import { EventWizardPage } from './pages/EventWizardPage';
import {
  CalendarDays,
  CheckCircle,
  Compass,
  Layers,
  LogOut,
  Plus,
  Radio,
  Settings,
  Shield,
  Users,
} from 'lucide-react';
import { DEMO_ORGANIZER_TOKEN } from './services/api';

// ---- Inner layout (uses useLocation, so it must be inside BrowserRouter) ----
const AppShell: React.FC = () => {
  const location = useLocation();
  const [tokenCopied, setTokenCopied] = useState(false);

  const copyToken = () => {
    navigator.clipboard.writeText(DEMO_ORGANIZER_TOKEN);
    setTokenCopied(true);
    setTimeout(() => setTokenCopied(false), 2000);
  };

  const isWizard = location.pathname.startsWith('/events/new');

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col selection:bg-brand-500 selection:text-white">
      {/* Top Navbar */}
      <header className="bg-white border-b border-slate-200/80 sticky top-0 z-40">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
          {/* Logo & Platform Info */}
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-brand-700 to-indigo-500 flex items-center justify-center text-white shadow-md shadow-brand-500/20">
              <Layers className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-extrabold text-slate-900 tracking-tight text-lg">
                  EventCrew
                </span>
                <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-slate-100 text-slate-600 border border-slate-200">
                  Organizer Hub
                </span>
              </div>
              <p className="text-[11px] text-slate-400">
                Volunteer Lifecycle & Application Management
              </p>
            </div>
          </div>

          {/* Nav + User */}
          <div className="flex items-center space-x-4">
            {/* Primary navigation */}
            <nav className="hidden md:flex items-center space-x-1">
              <Link
                to="/"
                className={[
                  'px-3 py-1.5 rounded-lg text-sm font-medium transition-colors',
                  !isWizard
                    ? 'bg-brand-50 text-brand-700'
                    : 'text-slate-600 hover:bg-slate-100',
                ].join(' ')}
              >
                Applications
              </Link>
              <Link
                to="/events/new"
                className={[
                  'flex items-center px-3 py-1.5 rounded-lg text-sm font-medium transition-colors',
                  isWizard
                    ? 'bg-brand-600 text-white shadow-sm shadow-brand-500/20'
                    : 'text-slate-600 hover:bg-slate-100',
                ].join(' ')}
              >
                <Plus className="w-3.5 h-3.5 mr-1" />
                Create Event
              </Link>
            </nav>

            <div className="hidden md:flex items-center space-x-2 px-3 py-1.5 rounded-xl bg-slate-100/80 border border-slate-200 text-xs text-slate-600">
              <Radio className="w-3.5 h-3.5 text-emerald-500 animate-pulse" />
              <span>Backend:</span>
              <span className="font-mono text-slate-800 font-semibold">
                localhost:5100/api
              </span>
            </div>

            <button
              onClick={copyToken}
              className="hidden lg:flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-medium transition-colors"
              title="Copy active Organizer Bearer JWT token"
            >
              <Shield className="w-3.5 h-3.5 text-brand-600" />
              <span>{tokenCopied ? 'JWT Copied!' : 'Copy Organizer JWT'}</span>
            </button>

            {/* Organizer User Badge */}
            <div className="flex items-center space-x-2.5 pl-2 border-l border-slate-200">
              <div className="w-8 h-8 rounded-full bg-brand-600 text-white font-bold text-xs flex items-center justify-center">
                LO
              </div>
              <div className="hidden sm:block text-left">
                <div className="text-xs font-bold text-slate-800">Lead Organizer</div>
                <div className="text-[10px] text-emerald-600 font-semibold flex items-center">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 mr-1"></span>
                  Organizer Role
                </div>
              </div>
            </div>
          </div>
        </div>
      </header>

      {/* Main Page Content */}
      <main className="flex-1">
        <Routes>
          <Route path="/" element={<ApplicantManagementPage />} />
          <Route path="/events/new" element={<EventWizardPage />} />
        </Routes>
      </main>

      {/* Footer */}
      <footer className="bg-white border-t border-slate-200/80 py-6 mt-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500">
          <div className="flex items-center space-x-2">
            <span className="font-semibold text-slate-700">EventCrew Volunteer Platform</span>
            <span>•</span>
            <span>SE3090 • Student 1 (Events) + Student 2 (Applications)</span>
          </div>
          <div className="flex items-center space-x-4">
            <span className="flex items-center gap-1 text-emerald-600 font-medium">
              <CheckCircle className="w-3.5 h-3.5" />
              API Proxy Active (/api → 5100)
            </span>
            <span>Vite + React + Tailwind</span>
          </div>
        </div>
      </footer>
    </div>
  );
};

// ---- Root component ----
export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <AppShell />
    </BrowserRouter>
  );
};