import axios from 'axios';

/**
 * Axios client pointed at the backend API.
 * The base URL relies on the Vite proxy for /api → http://localhost:5100.
 */
export const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

/**
 * Request interceptor: attach JWT bearer token from localStorage.
 * Token is stored under 'token' key by authStore after a successful login.
 */
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token') ?? localStorage.getItem('jwt_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for consistent error extraction
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const customMessage =
      error.response?.data?.message ||
      error.response?.data?.title ||
      (error.response?.status === 404 ? 'Resource not found' : '') ||
      (error.response?.status === 401 ? 'Invalid email or password.' : '') ||
      (error.response?.status === 403 ? 'Forbidden: Organizer permission required' : '');

    if (customMessage) {
      error.message = customMessage;
    }
    return Promise.reject(error);
  }
);

// ── Legacy exports kept for backward compatibility ───────────────────────────
/** @deprecated Use authStore.login() instead */
export const getDevOrganizerToken = (): string => localStorage.getItem('token') ?? '';
/** @deprecated */
export const generateDevOrganizerToken = (): string => localStorage.getItem('token') ?? '';
/** @deprecated */
export const DEMO_ORGANIZER_TOKEN = '';
/** @deprecated */
export const DEV_JWT_SECRET = 'DevFallbackSecretKeyForLocalTestingOnly12345!';
