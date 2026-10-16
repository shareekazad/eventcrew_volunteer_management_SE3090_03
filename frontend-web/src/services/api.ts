import axios from 'axios';

// ── API base URL ────────────────────────────────────────────────────────────
// In dev: Vite's proxy handles /api → http://localhost:5100
// In prod: VITE_API_BASE_URL is set to the deployed Railway API URL
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

export const apiClient = axios.create({
  baseURL: API_BASE_URL ? `${API_BASE_URL}/api` : '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 60000,
});

// ── Auth helpers ────────────────────────────────────────────────────────────
// Reads the current JWT from localStorage. Returns null if not logged in.
export const getAuthToken = (): string | null => {
  return localStorage.getItem('token') || localStorage.getItem('jwt_token');
};

export const setAuthToken = (token: string): void => {
  localStorage.setItem('token', token);
};

export const clearAuthToken = (): void => {
  localStorage.removeItem('token');
  localStorage.removeItem('jwt_token');
};

/**
 * Demo helper: logs in as the seeded organizer using the real /api/Auth/login
 * endpoint. The returned token is signed by the backend and stored in
 * localStorage — nothing is hardcoded.
 *
 * Credentials come from the seeded DbInitializer (see backend README).
 * NOTE: john@eventcrew.com is the seeded organizer that exists in BOTH
 * the local Docker DB and the deployed Railway DB.
 */
export const autoLoginAsOrganizer = async (): Promise<string> => {
  const response = await apiClient.post('/Auth/login', {
    email: 'john@eventcrew.com',
    password: 'Organizer123!',
  });
  const token: string = response.data.token;
  setAuthToken(token);
  return token;
};

/**
 * Ensures we have a valid token. If none is stored, auto-logs in as the
 * demo organizer. Idempotent — call this on app startup.
 */
export const ensureAuthenticated = async (): Promise<string | null> => {
  const existing = getAuthToken();
  if (existing) return existing;

  try {
    return await autoLoginAsOrganizer();
  } catch (err) {
    console.warn('Auto-login failed:', err);
    return null;
  }
};

/**
 * Decodes the `sub` (user id) claim from the current JWT. Returns a fallback
 * organizer GUID if the token is missing or malformed.
 */
export const getCurrentUserId = (): string => {
  const token = getAuthToken();
  if (!token) return 'a0000000-0000-0000-0000-000000000001';
  try {
    const payload = token.split('.')[1];
    const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
    return decoded.sub as string;
  } catch {
    return 'a0000000-0000-0000-0000-000000000001';
  }
};

// ── Request interceptor: attach Authorization header if we have a token ──
apiClient.interceptors.request.use(
  (config) => {
    const token = getAuthToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// ── Response interceptor for consistent error extraction ──
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const message =
      error.response?.data?.message ||
      error.response?.data?.title ||
      (error.response?.status === 404 ? 'Resource not found' : '') ||
      (error.response?.status === 401 ? 'Unauthorized: Please log in' : '') ||
      (error.response?.status === 403 ? 'Forbidden: Organizer permission required' : '') ||
      error.message ||
      'An unexpected error occurred';
    return Promise.reject(new Error(message));
  }
);