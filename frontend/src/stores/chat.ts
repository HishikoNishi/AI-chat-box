import {
  HubConnection,
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel,
} from '@microsoft/signalr'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import * as chatApi from '@/api/chat'
import { getApiUrl } from '@/api/client'
import { useAuthStore } from '@/stores/auth'
import { normalizeMessage } from '@/utils/message'
import type {
  ChatMessage,
  ChatSession,
  MessageSavedPayload,
  MessageStartedPayload,
} from '@/types'

export const useChatStore = defineStore('chat', () => {
  const authStore = useAuthStore()

  const sessions = ref<ChatSession[]>([])
  const activeSessionId = ref<string | null>(null)
  const messages = ref<ChatMessage[]>([])
  const loadingSessions = ref(false)
  const loadingMessages = ref(false)
  const sending = ref(false)
  const streamingMessageId = ref<string | null>(null)
  const error = ref<string | null>(null)

  let connection: HubConnection | null = null

  const activeSession = computed(() =>
    sessions.value.find((session) => session.id === activeSessionId.value) ?? null,
  )

  function resetMessages(): void {
    messages.value = []
    streamingMessageId.value = null
  }

  async function loadSessions(): Promise<void> {
    const token = await authStore.ensureValidToken()
    if (!token) return

    loadingSessions.value = true
    error.value = null

    try {
      sessions.value = await chatApi.getSessions(token)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load sessions.'
    } finally {
      loadingSessions.value = false
    }
  }

  async function selectSession(sessionId: string): Promise<void> {
    if (activeSessionId.value === sessionId) return

    activeSessionId.value = sessionId
    resetMessages()

    const token = await authStore.ensureValidToken()
    if (!token) return

    loadingMessages.value = true
    error.value = null

    try {
      messages.value = await chatApi.getMessages(token, sessionId)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load messages.'
    } finally {
      loadingMessages.value = false
    }
  }

  async function createSession(title?: string): Promise<ChatSession | null> {
    const token = await authStore.ensureValidToken()
    if (!token) return null

    error.value = null

    try {
      const session = await chatApi.createSession(token, title)
      sessions.value = [session, ...sessions.value]
      await selectSession(session.id)
      return session
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to create session.'
      return null
    }
  }

function upsertMessage(message: ChatMessage): void {
  const normalized = normalizeMessage(message)

  const index = messages.value.findIndex(
    (item) => item.id === normalized.id
  )

  if (index >= 0) {
    messages.value[index] = {
      ...messages.value[index],
      ...normalized,
    }
    return
  }

  messages.value.push(normalized)
}

  function appendToken(messageId: string, token: string): void {
    const message = messages.value.find((item) => item.id === messageId)
    if (!message) return
    message.content += token
  }

  async function ensureConnection(): Promise<HubConnection | null> {
    const token = await authStore.ensureValidToken()
    if (!token) return null

    if (connection?.state === 'Connected') {
      return connection
    }

    if (connection) {
      await connection.stop()
      connection = null
    }

    connection = new HubConnectionBuilder()
      .withUrl(`${getApiUrl()}/hubs/chat`, {
        accessTokenFactory: async () => (await authStore.ensureValidToken()) ?? '',
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('MessageSaved', (payload: MessageSavedPayload) => {
      upsertMessage({
        id: payload.id,
        sessionId: payload.sessionId,
        role: payload.role,
        content: payload.content,
        createdAt: payload.createdAt,
        clientTempId: payload.clientTempId,
      })
    })

    connection.on('MessageStarted', (payload: MessageStartedPayload) => {
      streamingMessageId.value = payload.id
      upsertMessage({
        id: payload.id,
        sessionId: payload.sessionId,
        role: payload.role,
        content: '',
        createdAt: payload.createdAt,
      })
    })

    connection.on('ReceiveToken', (tokenChunk: string) => {
      if (!streamingMessageId.value) return
      appendToken(streamingMessageId.value, tokenChunk)
    })

    connection.on('StreamComplete', (payload: ChatMessage) => {
      upsertMessage(payload)      
      streamingMessageId.value = null
      sending.value = false
    })

    connection.on('ReceiveError', (message: string) => {
      error.value = message
      streamingMessageId.value = null
      sending.value = false
    })

    await connection.start()
    return connection
  }

  async function sendMessage(text: string): Promise<void> {
    if (!activeSessionId.value || !text.trim() || sending.value) return

    sending.value = true
    error.value = null

    try {
      const hub = await ensureConnection()
      if (!hub) {
        throw new Error('Unable to connect to chat hub.')
      }

      const clientTempId = crypto.randomUUID()
      await hub.invoke('SendMessage', activeSessionId.value, text.trim(), clientTempId)
    } catch (err) {
      sending.value = false
      streamingMessageId.value = null
      error.value = err instanceof Error ? err.message : 'Failed to send message.'
    }
  }

  async function disconnect(): Promise<void> {
    if (!connection) return
    await connection.stop()
    connection = null
  }

  function reset(): void {
    sessions.value = []
    activeSessionId.value = null
    resetMessages()
    error.value = null
    sending.value = false
  }

  return {
    sessions,
    activeSessionId,
    activeSession,
    messages,
    loadingSessions,
    loadingMessages,
    sending,
    streamingMessageId,
    error,
    loadSessions,
    selectSession,
    createSession,
    sendMessage,
    disconnect,
    reset,
  }
})
