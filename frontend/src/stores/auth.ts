import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import * as authApi from '@/api/auth'
import { ApiClientError } from '@/api/client'
import type { AuthResponse, User } from '@/types'

const STORAGE_KEY = 'aichat.auth'

interface StoredAuth {
  accessToken: string
  accessTokenExpiresAt: string
  user: User
}

function loadStoredAuth(): StoredAuth | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null

  try {
    return JSON.parse(raw) as StoredAuth
  } catch {
    localStorage.removeItem(STORAGE_KEY)
    return null
  }
}

function persistAuth(auth: StoredAuth | null): void {
  if (!auth) {
    localStorage.removeItem(STORAGE_KEY)
    return
  }

  localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
}

export const useAuthStore = defineStore('auth', () => {
  const stored = loadStoredAuth()
  const accessToken = ref<string | null>(stored?.accessToken ?? null)
  const accessTokenExpiresAt = ref<string | null>(stored?.accessTokenExpiresAt ?? null)
  const user = ref<User | null>(stored?.user ?? null)
  const loading = ref(false)

  const isAuthenticated = computed(() => Boolean(accessToken.value && user.value))

  function applyAuth(response: AuthResponse): void {
    accessToken.value = response.accessToken
    accessTokenExpiresAt.value = response.accessTokenExpiresAt
    user.value = response.user
    persistAuth({
      accessToken: response.accessToken,
      accessTokenExpiresAt: response.accessTokenExpiresAt,
      user: response.user,
    })
  }

  function clearAuth(): void {
    accessToken.value = null
    accessTokenExpiresAt.value = null
    user.value = null
    persistAuth(null)
  }

  function isTokenExpired(bufferSeconds = 30): boolean {
    if (!accessTokenExpiresAt.value) return true
    const expiresAt = new Date(accessTokenExpiresAt.value).getTime()
    return Date.now() >= expiresAt - bufferSeconds * 1000
  }

  async function register(email: string, password: string): Promise<void> {
    loading.value = true
    try {
      applyAuth(await authApi.register({ email, password }))
    } finally {
      loading.value = false
    }
  }

  async function login(email: string, password: string): Promise<void> {
    loading.value = true
    try {
      applyAuth(await authApi.login({ email, password }))
    } finally {
      loading.value = false
    }
  }

  async function refreshSession(): Promise<boolean> {
    try {
      applyAuth(await authApi.refresh())
      return true
    } catch (error) {
      if (error instanceof ApiClientError && error.status === 401) {
        clearAuth()
      }
      return false
    }
  }

  async function ensureValidToken(): Promise<string | null> {
    if (!accessToken.value) return null
    if (!isTokenExpired()) return accessToken.value

    const refreshed = await refreshSession()
    return refreshed ? accessToken.value : null
  }

  async function logout(): Promise<void> {
    try {
      await authApi.logout()
    } finally {
      clearAuth()
    }
  }

  return {
    accessToken,
    accessTokenExpiresAt,
    user,
    loading,
    isAuthenticated,
    register,
    login,
    refreshSession,
    ensureValidToken,
    isTokenExpired,
    logout,
    clearAuth,
  }
})
