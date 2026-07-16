<script setup lang="ts">
import { ref, watch } from 'vue'
import { Send, Loader2, Sparkles } from '@lucide/vue'
import { useChatStore } from '@/stores/chat'

const chatStore = useChatStore()
const draft = ref('')
const textareaRef = ref<HTMLTextAreaElement | null>(null)

function autoResize(): void {
  const el = textareaRef.value
  if (!el) return
  el.style.height = 'auto'
  el.style.height = `${Math.min(el.scrollHeight, 150)}px`
}

watch(draft, () => autoResize())

async function handleSubmit(): Promise<void> {
  const text = draft.value
  if (!text.trim()) return

  draft.value = ''
  autoResize()
  await chatStore.sendMessage(text)
}
</script>

<template>
  <form class="chat-input" @submit.prevent="handleSubmit">
    <p v-if="chatStore.activeSessionId" class="chat-input-hint">
      <Sparkles :size="12" />
      Nhấn Enter để gửi · AI trả lời realtime
    </p>

    <div class="chat-input-box">
      <textarea
        ref="textareaRef"
        v-model="draft"
        rows="1"
        placeholder="Nhập tin nhắn cho AI..."
        :disabled="!chatStore.activeSessionId || chatStore.sending"
        @input="autoResize"
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
