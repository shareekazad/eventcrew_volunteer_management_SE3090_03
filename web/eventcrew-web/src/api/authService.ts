import { apiClient } from './client'
import type { AuthenticatedUser, LoginRequest, LoginResponse } from '../types/auth'

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/auth/login', request)
  return response.data
}

export async function getCurrentUser(): Promise<AuthenticatedUser> {
  const response = await apiClient.get<AuthenticatedUser>('/auth/me')
  return response.data
}
