<script setup lang="ts">
import { Bot, Plus, MessageCircle, Clock, LogOut, User } from '@lucide/vue'
import { useChatStore } from '@/stores/chat'
import { useAuthStore } from '@/stores/auth'

const emit = defineEmits<{
  logout: []
}>()

const chatStore = useChatStore()
const authStore = useAuthStore()

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

async function handleCreateSession(): Promise<void> {
  await chatStore.createSession('Cuộc trò chuyện mới')
}
</script>

<template>
  <aside class="chat-sidebar">
    <div class="sidebar-brand">
      <div class="sidebar-brand-icon">
        <Bot :size="22" :stroke-width="2" />
      </div>
      <div class="sidebar-brand-text">
        <h2>AI Chat Box</h2>
        <span>Trò chuyện thông minh</span>
      </div>
    </div>

    <div class="chat-sidebar-header">
      <h3>
        Cuộc trò chuyện
        <span v-if="chatStore.sessions.length" class="session-count">{{ chatStore.sessions.length }}</span>
      </h3>
      <button class="btn btn-secondary" type="button" @click="handleCreateSession">
        <Plus :size="16" />
        Mới
      </button>
    </div>

    <div class="session-list">
      <button
        v-for="session in chatStore.sessions"
        :key="session.id"
        type="button"
        class="session-item"
        :class="{ active: session.id === chatStore.activeSessionId }"
        @click="chatStore.selectSession(session.id)"
      >
        <div class="session-item-icon">
          <MessageCircle :size="15" />
        </div>
        <div class="session-item-body">
          <span class="session-item-title">{{ session.title }}</span>
          <span class="session-item-date">
            <Clock :size="11" />
            {{ formatDate(session.updatedAt) }}
          </span>
        </div>
      </button>

      <div v-if="!chatStore.loadingSessions && chatStore.sessions.length === 0" class="chat-empty">
        <div class="chat-empty-icon">
          <MessageCircle :size="28" />
        </div>
        <p>Chưa có cuộc trò chuyện nào.<br />Nhấn <strong>Mới</strong> để bắt đầu nhé!</p>
      </div>
    </div>

    <div class="user-panel">
      <div class="user-avatar">
        <User :size="18" />
      </div>
      <div class="user-info">
        <span class="user-email">{{ authStore.user?.email }}</span>
        <span class="user-label">Tài khoản của bạn</span>
      </div>
      <button class="btn btn-ghost" type="button" title="Đăng xuất" @click="emit('logout')">
        <LogOut :size="18" />
      </button>
    </div>
  </aside>
</template>
