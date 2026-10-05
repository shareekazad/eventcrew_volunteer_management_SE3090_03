import axios, { isAxiosError } from 'axios'
import { clearAccessToken, getAccessToken } from '../auth/tokenStorage'

const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5100'
export const AUTH_SESSION_EXPIRED_EVENT = 'eventcrew:auth-session-expired'

export const apiClient = axios.create({
  baseURL: import.meta.env.DEV ? '/api' : configuredApiBaseUrl,
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) config.headers.set('Authorization', `Bearer ${token}`)
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (isAxiosError(error) && error.response?.status === 401 && !error.config?.url?.includes('/auth/login')) {
      clearAccessToken()
      if (typeof window !== 'undefined') window.dispatchEvent(new Event(AUTH_SESSION_EXPIRED_EVENT))
    }
    return Promise.reject(error)
  },
)

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function getApiErrorMessage(error: unknown): string {
  if (!isAxiosError<ProblemDetails>(error)) return 'Something went wrong. Please try again.'
  if (!error.response) return 'Could not connect to EventCrew. Check that the API is running and try again.'

  const { status, data } = error.response
  if (status === 400) {
    const validationMessages = Object.values(data?.errors ?? {}).flat()
    return validationMessages[0] ?? data?.detail ?? 'Some shift details are invalid. Review the form and try again.'
  }
  if (status === 404) return data?.detail ?? 'The shift, event, or role requirement could not be found. Refresh and try again.'
  if (status === 409) return data?.detail ?? 'This shift conflicts with another change. Refresh the list and try again.'
  if (status >= 500) return 'EventCrew could not complete the request because of a server error. Try again later.'
  return data?.detail ?? data?.title ?? `The request failed (${status}). Please try again.`
}
