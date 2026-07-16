import type { ApiError } from '@/types'

const API_URL = (import.meta.env.VITE_API_URL ?? 'https://localhost:7219').replace(/\/$/, '')

export class ApiClientError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiClientError'
    this.status = status
  }
}

type RequestOptions = {
  method?: string
  body?: unknown
  token?: string | null
  credentials?: RequestCredentials
  headers?: Record<string, string>
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {
    ...(options.headers ?? {}),
  }

  if (!(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json'
  }

  if (options.token) {
    headers.Authorization = `Bearer ${options.token}`
  }

  const response = await fetch(`${API_URL}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body instanceof FormData
      ? options.body
      : options.body !== undefined
        ? JSON.stringify(options.body)
        : undefined,
    credentials: options.credentials ?? 'include',
  })

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  const payload = text ? (JSON.parse(text) as T | ApiError) : undefined

  if (!response.ok) {
    const message =
      payload && typeof payload === 'object' && 'message' in payload
        ? String((payload as ApiError).message)
        : `Request failed (${response.status})`
    throw new ApiClientError(response.status, message)
  }

  return payload as T
}

export function getApiUrl(): string {
  return API_URL
}

export function buildAttachmentUrl(relativeUrl: string, token: string): string {
  const url = relativeUrl.startsWith('http') ? relativeUrl : `${API_URL}${relativeUrl}`
  return `${url}?access_token=${encodeURIComponent(token)}`
}
