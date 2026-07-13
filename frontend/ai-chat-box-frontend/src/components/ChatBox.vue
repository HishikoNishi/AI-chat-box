<template>
  <div class="chat-container" style="padding: 20px; max-width: 800px; margin: auto;">
    <div class="messages" style="margin-bottom: 20px; min-height: 300px; border: 1px solid var(--border); padding: 10px; overflow-y: auto;">
      <div v-for="msg in chatStore.messages" :key="msg.id" :class="['message', msg.sender.toLowerCase()]" style="margin-bottom: 8px;">
        <strong>{{ msg.sender }}:</strong> {{ msg.content }}
      </div>
    </div>
    <form @submit.prevent="sendMessage" style="display: flex; gap: 8px;">
      <input v-model="newMessage" placeholder="Type a message" style="flex: 1;" />
      <button type="submit" class="btn btn-primary">Send</button>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useChatStore } from '@/stores/chat';
import { useSignalR } from '@/signalr/connection';

const chatStore = useChatStore();
const { startConnection, sendMessageToHub } = useSignalR();
const newMessage = ref('');

onMounted(async () => {
  await startConnection();
});

async function sendMessage() {
  if (!newMessage.value.trim()) return;
  const content = newMessage.value.trim();
  chatStore.addMessage({ id: Date.now(), sender: 'Me', content });
  await sendMessageToHub(content);
  newMessage.value = '';
}
</script>

<style scoped>
.message.me { color: var(--accent); }
.message.bot { color: var(--text); }
</style>
