/**
 * authStore.ts — SE3090 Section 7 & 17.1
 * Zustand store managing authentication state, persisted in localStorage.
 */
import { create } from 'zustand';
import axios from 'axios';
import { apiClient } from '../services/api';

export interface AuthUser {
  token: string;
  role: string;
  fullName: string;
  email: string;
  userId: string;
}

interface AuthState {
  user: AuthUser | null;
  isLoading: boolean;
  error: string | null;

  // Actions
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  clearError: () => void;
  hydrateFromStorage: () => void;
}

const AUTH_STORAGE_KEY = 'eventcrew_auth';

/** Persist user to localStorage */
const persistUser = (user: AuthUser) => {
  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(user));
  // Also set 'token' key so existing apiClient interceptor picks it up immediately
  localStorage.setItem('token', user.token);
};

/** Clear user from localStorage */
const clearStorage = () => {
  localStorage.removeItem(AUTH_STORAGE_KEY);
  localStorage.removeItem('token');
  localStorage.removeItem('jwt_token');
};

/** Read user from localStorage on app boot */
const readFromStorage = (): AuthUser | null => {
  try {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
};

export const useAuthStore = create<AuthState>((set) => ({
  user: readFromStorage(),
  isLoading: false,
  error: null,

  hydrateFromStorage: () => {
    const user = readFromStorage();
    set({ user });
  },

  login: async (email: string, password: string) => {
    set({ isLoading: true, error: null });

    try {
      const response = await apiClient.post<AuthUser>(
        '/auth/login',
        { email, password }
      );

      const user = response.data;

      // Enforce Organizer/Admin portal — volunteers are rejected
      const role = user.role ?? '';
      if (role !== 'Organizer' && role !== 'Admin') {
        set({
          isLoading: false,
          error: `Access denied. This portal is for Organizers only. Your role is "${role}".`,
        });
        return;
      }

      persistUser(user);
      set({ user, isLoading: false, error: null });
    } catch (err: unknown) {
      let message = 'Login failed. Please check your credentials.';
      if (axios.isAxiosError(err)) {
        if (!err.response) {
          message = 'Cannot connect to server. Is the backend running on port 5100?';
        } else if (err.response.status === 401) {
          message = (err.response.data as { message?: string })?.message ?? 'Invalid email or password.';
        } else if ((err.response.data as { message?: string })?.message) {
          message = (err.response.data as { message?: string }).message!;
        } else {
          message = err.message || message;
        }
      } else if (err instanceof Error) {
        message = err.message;
      }
      set({ isLoading: false, error: message });
    }
  },

  logout: () => {
    clearStorage();
    set({ user: null, error: null });
  },

  clearError: () => set({ error: null }),
}));
