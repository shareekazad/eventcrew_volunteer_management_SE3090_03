import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Calendar, Mail, Lock, LogIn, Zap, AlertCircle, Eye, EyeOff, Loader2 } from 'lucide-react';
import { useAuthStore } from '../stores/authStore';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login, isLoading, error, clearError } = useAuthStore();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});

  /** Basic client-side validation */
  const validate = (): boolean => {
    const errs: { email?: string; password?: string } = {};
    if (!email.trim()) errs.email = 'Email is required.';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) errs.email = 'Invalid email address.';
    if (!password) errs.password = 'Password is required.';
    setFieldErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    if (!validate()) return;
    await login(email.trim(), password);
    // Navigate on success (store will have user set)
    if (useAuthStore.getState().user) {
      navigate('/applicants', { replace: true });
    }
  };

  /** Prefill demo credentials and auto-submit */
  const handleQuickDemo = async () => {
    clearError();
    setFieldErrors({});
    setEmail('john@eventcrew.com');
    setPassword('password123');
    // Small tick to let state update reflect in UI, then submit
    await login('john@eventcrew.com', 'password123');
    if (useAuthStore.getState().user) {
      navigate('/applicants', { replace: true });
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-[#071F18] via-[#0F382C] to-[#0D2E23] flex items-center justify-center p-4 selection:bg-[#3ECFA0] selection:text-[#0F382C]">
      {/* Ambient background blobs */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-32 w-[500px] h-[500px] bg-[#3ECFA0]/5 rounded-full blur-3xl" />
        <div className="absolute -bottom-40 -left-32 w-[500px] h-[500px] bg-[#3ECFA0]/5 rounded-full blur-3xl" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[800px] h-[800px] bg-[#3ECFA0]/3 rounded-full blur-3xl" />
      </div>

      <div className="relative w-full max-w-md">
        {/* Card */}
        <div className="bg-[#0D2E23]/80 backdrop-blur-xl border border-[#1C4B3D]/60 rounded-3xl shadow-2xl shadow-black/40 overflow-hidden">
          {/* Top accent stripe */}
          <div className="h-1 bg-gradient-to-r from-transparent via-[#3ECFA0] to-transparent" />

          <div className="p-8 pt-10">
            {/* Brand Header */}
            <div className="flex flex-col items-center mb-10">
              <div className="w-16 h-16 rounded-2xl bg-[#3ECFA0] flex items-center justify-center shadow-lg shadow-[#3ECFA0]/20 mb-4">
                <Calendar className="w-8 h-8 text-[#0F382C]" strokeWidth={2.5} />
              </div>
              <h1 className="text-2xl font-bold text-white tracking-tight">EventCrew</h1>
              <p className="text-[#7AA493] text-sm mt-1 font-medium">Organizer Portal</p>
            </div>

            {/* Error Banner */}
            {error && (
              <div className="mb-6 flex items-start gap-3 bg-red-950/50 border border-red-800/60 rounded-xl px-4 py-3 text-red-300 text-sm animate-in slide-in-from-top-1 duration-200">
                <AlertCircle className="w-4 h-4 shrink-0 mt-0.5 text-red-400" />
                <span>{error}</span>
              </div>
            )}

            {/* Quick Demo Pill */}
            <button
              type="button"
              onClick={handleQuickDemo}
              disabled={isLoading}
              className="w-full mb-6 flex items-center justify-center gap-2 bg-[#3ECFA0]/10 hover:bg-[#3ECFA0]/20 border border-[#3ECFA0]/30 hover:border-[#3ECFA0]/50 text-[#3ECFA0] text-sm font-semibold py-2.5 rounded-xl transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed group"
            >
              <Zap className="w-4 h-4 group-hover:animate-pulse" />
              ⚡ Quick Demo Login: John (Organizer)
            </button>

            <div className="flex items-center gap-3 mb-6">
              <div className="flex-1 h-px bg-[#1C4B3D]" />
              <span className="text-[#4E8070] text-xs font-medium">or sign in manually</span>
              <div className="flex-1 h-px bg-[#1C4B3D]" />
            </div>

            {/* Form */}
            <form onSubmit={handleSubmit} noValidate className="space-y-5">
              {/* Email Field */}
              <div>
                <label htmlFor="login-email" className="block text-xs font-semibold text-[#9BB8AC] mb-1.5 uppercase tracking-wider">
                  Email Address
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none">
                    <Mail className="w-4 h-4 text-[#4E8070]" />
                  </div>
                  <input
                    id="login-email"
                    type="email"
                    autoComplete="email"
                    value={email}
                    onChange={(e) => { setEmail(e.target.value); setFieldErrors((p) => ({ ...p, email: undefined })); }}
                    placeholder="you@eventcrew.com"
                    className={`w-full pl-10 pr-4 py-3 rounded-xl bg-[#071F18]/70 border text-white placeholder-[#3D6B58] text-sm outline-none transition-all duration-200 focus:ring-2 focus:ring-[#3ECFA0]/30 ${
                      fieldErrors.email
                        ? 'border-red-700/70 focus:border-red-600'
                        : 'border-[#1C4B3D] focus:border-[#3ECFA0]/60'
                    }`}
                  />
                </div>
                {fieldErrors.email && (
                  <p className="mt-1.5 text-xs text-red-400">{fieldErrors.email}</p>
                )}
              </div>

              {/* Password Field */}
              <div>
                <label htmlFor="login-password" className="block text-xs font-semibold text-[#9BB8AC] mb-1.5 uppercase tracking-wider">
                  Password
                </label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none">
                    <Lock className="w-4 h-4 text-[#4E8070]" />
                  </div>
                  <input
                    id="login-password"
                    type={showPassword ? 'text' : 'password'}
                    autoComplete="current-password"
                    value={password}
                    onChange={(e) => { setPassword(e.target.value); setFieldErrors((p) => ({ ...p, password: undefined })); }}
                    placeholder="••••••••"
                    className={`w-full pl-10 pr-11 py-3 rounded-xl bg-[#071F18]/70 border text-white placeholder-[#3D6B58] text-sm outline-none transition-all duration-200 focus:ring-2 focus:ring-[#3ECFA0]/30 ${
                      fieldErrors.password
                        ? 'border-red-700/70 focus:border-red-600'
                        : 'border-[#1C4B3D] focus:border-[#3ECFA0]/60'
                    }`}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword((v) => !v)}
                    className="absolute inset-y-0 right-0 pr-3.5 flex items-center text-[#4E8070] hover:text-[#9BB8AC] transition-colors"
                    tabIndex={-1}
                    aria-label="Toggle password visibility"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
                {fieldErrors.password && (
                  <p className="mt-1.5 text-xs text-red-400">{fieldErrors.password}</p>
                )}
              </div>

              {/* Submit Button */}
              <button
                id="login-submit-btn"
                type="submit"
                disabled={isLoading}
                className="w-full mt-2 flex items-center justify-center gap-2.5 bg-[#3ECFA0] hover:bg-[#35C090] active:bg-[#2BAA7E] text-[#071F18] font-bold py-3.5 rounded-xl transition-all duration-200 shadow-lg shadow-[#3ECFA0]/20 disabled:opacity-60 disabled:cursor-not-allowed text-sm"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Signing In…
                  </>
                ) : (
                  <>
                    <LogIn className="w-4 h-4" strokeWidth={2.5} />
                    Sign In
                  </>
                )}
              </button>
            </form>
          </div>

          {/* Footer Note */}
          <div className="px-8 pb-7 pt-0 text-center">
            <p className="text-[#3D6B58] text-xs">
              SE3090 — EventCrew Volunteer Management System
            </p>
            <p className="text-[#2E5444] text-[10px] mt-0.5">
              Organizer &amp; Admin access only
            </p>
          </div>
        </div>

        {/* Floating demo hint below card */}
        <p className="text-center text-[#3D6B58] text-xs mt-5">
          Demo credentials: <span className="text-[#5A9B7E]">john@eventcrew.com</span> / <span className="text-[#5A9B7E]">password123</span>
        </p>
      </div>
    </div>
  );
};
