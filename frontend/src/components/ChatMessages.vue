<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
import { Bot, User, Clock, Sparkles, Loader2 } from '@lucide/vue'
import { useChatStore } from '@/stores/chat'
import { roleCssClass } from '@/utils/message'

const chatStore = useChatStore()
const messagesEl = ref<HTMLElement | null>(null)

function formatTime(value: string): string {
  return new Intl.DateTimeFormat('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    day: '2-digit',
    month: '2-digit',
  }).format(new Date(value))
}

function isStreaming(messageId: string): boolean {
  return messageId === chatStore.streamingMessageId && chatStore.sending
}

async function scrollToBottom(): Promise<void> {
  await nextTick()
  if (messagesEl.value) {
    messagesEl.value.scrollTop = messagesEl.value.scrollHeight
  }
}

watch(
  () => [chatStore.messages.length, chatStore.sending, chatStore.streamingMessageId],
  () => scrollToBottom(),
)
</script>

<template>
  <section ref="messagesEl" class="chat-messages">
    <div v-if="chatStore.loadingMessages" class="chat-empty">
      <div class="chat-empty-icon">
        <Loader2 :size="28" class="spin" />
      </div>
      <p>Đang tải tin nhắn...</p>
    </div>

    <div v-else-if="!chatStore.activeSessionId" class="chat-empty">
      <div class="chat-empty-icon">
        <Sparkles :size="30" />
      </div>
      <h2>Chào mừng đến AI Chat Box!</h2>
      <p>Chọn một cuộc trò chuyện hoặc tạo mới để bắt đầu trò chuyện cùng AI.</p>
    </div>

    <template v-else>
      <div
        v-for="message in chatStore.messages"
        :key="message.id"
        class="message-row"
        :class="roleCssClass(message.role)"
      >
        <div class="message-avatar">
          <User v-if="message.role === 'User'" :size="17" />
          <Bot v-else :size="17" />
        </div>

        <div class="message-bubble">
          <template v-if="isStreaming(message.id) && !message.content">
            <span class="typing-dots">
              <span /><span /><span />
            </span>
          </template>
          <template v-else>
            {{ message.content || '...' }}
          </template>
          <span class="message-meta">
            <Clock :size="11" />
            {{ formatTime(message.createdAt) }}
          </span>
        </div>
      </div>

      <div v-if="chatStore.messages.length === 0" class="chat-empty">
        <div class="chat-empty-icon">
          <Bot :size="30" />
        </div>
        <h2>Bắt đầu trò chuyện</h2>
        <p>Hãy gửi tin nhắn đầu tiên — AI đang chờ bạn đó!</p>
      </div>
    </template>
  </section>
</template>

<style scoped>
.spin {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
.chat-messages {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 20px;
  overflow-y: auto;
}

.message-row {
  display: flex;
  width: 100%;
}


/* User bên phải */
.message-row.user {
  justify-content: flex-end;
}


/* AI bên trái */
.message-row.assistant {
  justify-content: flex-start;
}


.message-row.user .message-bubble {
  background: #2563eb;
  color: white;
  border-radius: 18px 18px 4px 18px;
}


.message-row.assistant .message-bubble {
  background: #e5e7eb;
  color: #111827;
  border-radius: 18px 18px 18px 4px;
}


.message-bubble {
  max-width: 70%;
  padding: 12px 16px;
}
</style>
