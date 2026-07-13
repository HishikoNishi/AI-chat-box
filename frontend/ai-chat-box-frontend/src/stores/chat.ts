import { defineStore } from 'pinia';

export interface ChatMessage {
  id: number;
  sender: string;
  content: string;
}

export const useChatStore = defineStore('chat', {
  state: () => ({
    messages: [] as ChatMessage[],
  }),
  actions: {
    addMessage(msg: ChatMessage) {
      this.messages.push(msg);
    },
    clear() {
      this.messages = [];
    },
  },
});
