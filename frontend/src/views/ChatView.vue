<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { MessageSquare, AlertCircle, Sparkles } from '@lucide/vue'
import ChatInput from '@/components/ChatInput.vue'
import ChatMessages from '@/components/ChatMessages.vue'
import ChatSidebar from '@/components/ChatSidebar.vue'
import { useAuthStore } from '@/stores/auth'
import { useChatStore } from '@/stores/chat'

const authStore = useAuthStore()
const chatStore = useChatStore()
const router = useRouter()

onMounted(async () => {
  document.documentElement.classList.add('chat-active')
  await chatStore.loadSessions()
})

onUnmounted(async () => {
  document.documentElement.classList.remove('chat-active')
  await chatStore.disconnect()
})

async function handleLogout(): Promise<void> {
  await chatStore.disconnect()
  chatStore.reset()
  await authStore.logout()
  await router.push('/login')
}
</script>

<template>
  <div class="chat-layout">
    <ChatSidebar @logout="handleLogout" />

    <section class="chat-main">
      <header class="chat-header">
        <div class="chat-header-info">
          <div class="chat-header-icon">
            <MessageSquare :size="20" :stroke-width="2" />
          </div>
          <div>
            <h1>{{ chatStore.activeSession?.title ?? 'AI Chat Box' }}</h1>
            <p class="chat-header-meta">
              <Sparkles :size="12" />
              {{ authStore.user?.email }}
            </p>
          </div>
        </div>

        <div v-if="chatStore.activeSessionId" class="chat-header-status">
          <span class="status-dot" />
          Sẵn sàng
        </div>
      </header>

      <div class="chat-body">
        <p v-if="chatStore.error" class="error-banner chat-error-inline">
          <AlertCircle :size="18" :stroke-width="2" style="flex-shrink:0;margin-top:1px" />
          {{ chatStore.error }}
        </p>

        <ChatMessages />
      </div>

      <ChatInput />
    </section>
  </div>
</template>
