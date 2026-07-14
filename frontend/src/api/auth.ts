import { apiRequest } from '@/api/client'
import type { AuthResponse } from '@/types'

export interface CredentialsPayload {
  email: string
  password: string
}

export function register(payload: CredentialsPayload): Promise<AuthResponse> {
  return apiRequest<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: payload,
  })
}

export function login(payload: CredentialsPayload): Promise<AuthResponse> {
  return apiRequest<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: payload,
  })
}

export function refresh(): Promise<AuthResponse> {
  return apiRequest<AuthResponse>('/api/auth/refresh', {
    method: 'POST',
  })
}

export function logout(): Promise<void> {
  return apiRequest<void>('/api/auth/logout', {
    method: 'POST',
  })
}
