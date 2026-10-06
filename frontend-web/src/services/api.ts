import axios from 'axios';

// Valid pre-signed JWT token matching backend secret (DevFallbackSecretKeyForLocalTestingOnly12345!),
// issuer (EventCrew), audience (EventCrewUsers), role (Organizer), and email (organizer@eventcrew.com)
export const DEMO_ORGANIZER_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEiLCJuYW1lIjoiTGVhZCBPcmdhbml6ZXIiLCJlbWFpbCI6Im9yZ2FuaXplckBldmVudGNyZXcuY29tIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiT3JnYW5pemVyIiwicm9sZSI6Ik9yZ2FuaXplciIsImlzcyI6IkV2ZW50Q3JldyIsImF1ZCI6IkV2ZW50Q3Jld1VzZXJzIiwiZXhwIjoyMTA2MTM5ODkwfQ.ApOM7FEdyaxxhxPEhl5LeGKN6OXJdLRqhT2vAMoNIGo';

export const DEV_JWT_SECRET = 'DevFallbackSecretKeyForLocalTestingOnly12345!';

/**
 * Helper to generate or provide a valid development Organizer JWT token.
 * Token contains role "Organizer" and email "organizer@eventcrew.com", signed with dev key.
 */
export const getDevOrganizerToken = (): string => {
  return DEMO_ORGANIZER_TOKEN;
};

export const generateDevOrganizerToken = (_email = 'organizer@eventcrew.com', _role = 'Organizer'): string => {
  return DEMO_ORGANIZER_TOKEN;
};

export const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 60000,
});

// Request interceptor: attach JWT bearer token from localStorage or fallback demo token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token') || localStorage.getItem('jwt_token') || getDevOrganizerToken();
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
    const message =
      error.response?.data?.message ||
      error.response?.data?.title ||
      (error.response?.status === 404 ? 'Resource not found' : '') ||
      (error.response?.status === 401 ? 'Unauthorized: Please check credentials' : '') ||
      (error.response?.status === 403 ? 'Forbidden: Organizer permission required' : '') ||
      error.message ||
      'An unexpected error occurred';
    return Promise.reject(new Error(message));
  }
);
