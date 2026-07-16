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

function senderLabel(role: string): string {
  return role === 'User' ? 'Bạn' : 'AI Assistant'
}

async function scrollToBottom(): Promise<void> {
  await nextTick()
  if (messagesEl.value) {
    messagesEl.value.scrollTo({
      top: messagesEl.value.scrollHeight,
      behavior: 'smooth',
    })
  }
}

watch(
  () => [
    chatStore.messages.length,
    chatStore.sending,
    chatStore.streamingMessageId,
    chatStore.messages.at(-1)?.content,
  ],
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

        <div class="message-content">
          <span class="message-sender">{{ senderLabel(message.role) }}</span>

          <div
            class="message-bubble"
            :class="{ streaming: isStreaming(message.id) }"
          >
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
