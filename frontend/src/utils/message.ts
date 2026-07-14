import type { ChatMessage, MessageRole } from '@/types'

export function normalizeRole(role: unknown): MessageRole {
  if (role === 'User' || role === 1) return 'User'
  if (role === 'Assistant' || role === 2) return 'Assistant'
  if (typeof role === 'string') {
    const lower = role.toLowerCase()
    if (lower === 'user') return 'User'
    if (lower === 'assistant') return 'Assistant'
  }
  return 'User'
}

export function normalizeMessage(message: Partial<ChatMessage> & Pick<ChatMessage, 'id' | 'sessionId'>): ChatMessage {
  return {
    id: message.id,
    sessionId: message.sessionId,
    role: normalizeRole(message.role),
    content: message.content ?? '',
    createdAt:
      typeof message.createdAt === 'string'
        ? message.createdAt
        : new Date(message.createdAt ?? Date.now()).toISOString(),
    clientTempId: message.clientTempId,
  }
}

export function roleCssClass(role: MessageRole): string {
  return role === 'User' ? 'user' : 'assistant'
}
