import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Sidebar } from './components/Sidebar';
import { Header } from './components/Header';
import { ApplicantManagementPage } from './pages/ApplicantManagementPage';
import { AIMatchingPage } from './pages/AIMatchingPage';
import { ShiftSwapsPreviewPage } from './pages/ShiftSwapsPreviewPage';
import { LoginPage } from './pages/LoginPage';
import { useAuthStore } from './stores/authStore';

/**
 * ProtectedRoute — Redirects unauthenticated users to /login.
 * SE3090 Section 7 & 17.1: all workspace routes require authentication.
 */
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const user = useAuthStore((s) => s.user);
  if (!user) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
};

/**
 * The full authenticated workspace layout (sidebar + header + main content).
 */
const WorkspaceLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div className="min-h-screen bg-[#F8FAF9] flex selection:bg-[#0F382C] selection:text-mint">
    {/* Left Dark Pine Green Sidebar */}
    <Sidebar />

    {/* Right Main Application Area */}
    <div className="flex-1 flex flex-col min-w-0">
      {/* Top Breadcrumb & User Bar */}
      <Header />

      {/* Page Content Viewport */}
      <main className="flex-1 p-8 overflow-y-auto">
        {children}
      </main>
    </div>
  </div>
);

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public route — login */}
        <Route path="/login" element={<LoginPage />} />

        {/* Protected workspace routes */}
        <Route
          path="/applicants"
          element={
            <ProtectedRoute>
              <WorkspaceLayout>
                <ApplicantManagementPage />
              </WorkspaceLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/ai-matching"
          element={
            <ProtectedRoute>
              <WorkspaceLayout>
                <AIMatchingPage />
              </WorkspaceLayout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/shift-swaps"
          element={
            <ProtectedRoute>
              <WorkspaceLayout>
                <ShiftSwapsPreviewPage />
              </WorkspaceLayout>
            </ProtectedRoute>
          }
        />
        {/* Dashboard / Shifts / Assignments redirect to applicants (not yet implemented) */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <Navigate to="/applicants" replace />
            </ProtectedRoute>
          }
        />
        <Route
          path="/shifts"
          element={
            <ProtectedRoute>
              <Navigate to="/shift-swaps" replace />
            </ProtectedRoute>
          }
        />
        <Route
          path="/assignments"
          element={
            <ProtectedRoute>
              <Navigate to="/applicants" replace />
            </ProtectedRoute>
          }
        />

        {/* Root: redirect authenticated users to workspace, unauthenticated to login */}
        <Route path="/" element={<Navigate to="/applicants" replace />} />

        {/* Catch-all fallback */}
        <Route path="*" element={<Navigate to="/applicants" replace />} />
      </Routes>
    </BrowserRouter>
  );
};
