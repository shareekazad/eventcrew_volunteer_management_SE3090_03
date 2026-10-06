import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Sidebar } from './components/Sidebar';
import { Header } from './components/Header';
import { ApplicantManagementPage } from './pages/ApplicantManagementPage';
import { AIMatchingPage } from './pages/AIMatchingPage';
import { ShiftSwapsPreviewPage } from './pages/ShiftSwapsPreviewPage';

// Student 1 — Event & Venue management pages
import { EventsListPage } from './pages/EventsListPage';
import { EventDetailPage } from './pages/EventDetailPage';
import { EventWizardPage } from './pages/EventWizardPage';

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-[#F8FAF9] flex selection:bg-[#0F382C] selection:text-mint">
        {/* Left Dark Pine Green Sidebar */}
        <Sidebar />

        {/* Right Main Application Area */}
        <div className="flex-1 flex flex-col min-w-0">
          {/* Top Breadcrumb & User Bar */}
          <Header />

          {/* Page Content Viewport */}
          <main className="flex-1 p-8 overflow-y-auto">
            <Routes>
              {/* Default → redirect to applicants */}
              <Route path="/" element={<Navigate to="/applicants" replace />} />

              {/* Student 2 routes */}
              <Route path="/applicants" element={<ApplicantManagementPage />} />
              <Route path="/ai-matching" element={<AIMatchingPage />} />
              <Route path="/shift-swaps" element={<ShiftSwapsPreviewPage />} />

              {/* Student 1 routes — Events + Venues */}
              <Route path="/events" element={<EventsListPage />} />
              <Route path="/events/new" element={<EventWizardPage />} />
              <Route path="/events/:id" element={<EventDetailPage />} />

              {/* Fallback routes for other sidebar links */}
              <Route path="/dashboard" element={<Navigate to="/applicants" replace />} />
              <Route path="/shifts" element={<Navigate to="/shift-swaps" replace />} />
              <Route path="/assignments" element={<Navigate to="/applicants" replace />} />
              <Route path="*" element={<Navigate to="/applicants" replace />} />
            </Routes>
          </main>
        </div>
      </div>
    </BrowserRouter>
  );
};