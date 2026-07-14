export type MessageRole = 'User' | 'Assistant'

export interface User {
  id: string
  email: string
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  user: User
}

export interface ApiError {
  message: string
}

export interface ChatSession {
  id: string
  title: string
  createdAt: string
  updatedAt: string
}

export interface ChatMessage {
  id: string
  sessionId: string
  role: MessageRole
  content: string
  createdAt: string
  clientTempId?: string
}

export interface MessageSavedPayload extends ChatMessage {
  clientTempId?: string
}

export interface MessageStartedPayload {
  id: string
  sessionId: string
  role: MessageRole
  content: string
  createdAt: string
}
