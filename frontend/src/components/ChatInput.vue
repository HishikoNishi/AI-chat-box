<script setup lang="ts">
import { ref } from 'vue'
import { Send, Loader2 } from '@lucide/vue'
import { useChatStore } from '@/stores/chat'

const chatStore = useChatStore()
const draft = ref('')

async function handleSubmit(): Promise<void> {
  const text = draft.value
  if (!text.trim()) return

  draft.value = ''
  await chatStore.sendMessage(text)
}
</script>

<template>
  <form class="chat-input" @submit.prevent="handleSubmit">
    <div class="chat-input-box">
      <textarea
        v-model="draft"
        rows="1"
        placeholder="Nhập tin nhắn cho AI..."
        :disabled="!chatStore.activeSessionId || chatStore.sending"
        @keydown.enter.exact.prevent="handleSubmit"
      />

      <button
        class="btn btn-send"
        type="submit"
        :title="chatStore.sending ? 'AI đang trả lời...' : 'Gửi tin nhắn'"
        :disabled="!chatStore.activeSessionId || chatStore.sending || !draft.trim()"
      >
        <Loader2 v-if="chatStore.sending" :size="18" class="spin" />
        <Send v-else :size="18" />
      </button>
    </div>
  </form>
</template>

<style scoped>
.spin {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>
