import { apiRequest } from '@/api/client'
import type { ChatMessage, ChatSession } from '@/types'

export function getSessions(token: string): Promise<ChatSession[]> {
  return apiRequest<ChatSession[]>('/api/chat-sessions', { token })
}

export function createSession(token: string, title?: string): Promise<ChatSession> {
  return apiRequest<ChatSession>('/api/chat-sessions', {
    method: 'POST',
    token,
    body: { title: title ?? null },
  })
}

export function getMessages(token: string, sessionId: string): Promise<ChatMessage[]> {
  return apiRequest<ChatMessage[]>(`/api/chat-sessions/${sessionId}/messages`, { token })
}
