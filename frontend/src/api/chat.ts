import { apiRequest } from '@/api/client'
import type { ChatMessage, ChatSession, MessageAttachment } from '@/types'

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

export function deleteSession(token: string, sessionId: string): Promise<void> {
  return apiRequest<void>(`/api/chat-sessions/${sessionId}`, {
    method: 'DELETE',
    token,
  })
}

export function getMessages(token: string, sessionId: string): Promise<ChatMessage[]> {
  return apiRequest<ChatMessage[]>(`/api/chat-sessions/${sessionId}/messages`, { token })
}

export function uploadAttachment(
  token: string,
  sessionId: string,
  file: File,
): Promise<MessageAttachment> {
  const formData = new FormData()
  formData.append('file', file)

  return apiRequest<MessageAttachment>(`/api/chat-sessions/${sessionId}/attachments`, {
    method: 'POST',
    token,
    body: formData,
  })
}
