import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Sidebar } from './components/Sidebar';
import { Header } from './components/Header';
import { ApplicantManagementPage } from './pages/ApplicantManagementPage';
import { AIMatchingPage } from './pages/AIMatchingPage';
import { ShiftSwapsPreviewPage } from './pages/ShiftSwapsPreviewPage';

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
              <Route path="/" element={<Navigate to="/applicants" replace />} />
              <Route path="/applicants" element={<ApplicantManagementPage />} />
              <Route path="/ai-matching" element={<AIMatchingPage />} />
              <Route path="/shift-swaps" element={<ShiftSwapsPreviewPage />} />
              {/* Fallback routes for other sidebar preview links */}
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
