import * as signalR from '@microsoft/signalr';
import { useChatStore } from '@/stores/chat';

let hubConnection: signalR.HubConnection | null = null;

export function useSignalR() {
  const chatStore = useChatStore();

  const startConnection = async () => {
    if (hubConnection) return;
    hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:5001/chatHub') // Adjust URL as needed
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    hubConnection.on('ReceiveMessage', (user: string, message: string) => {
      chatStore.addMessage({ id: Date.now(), sender: user, content: message });
    });

    await hubConnection.start();
    console.log('SignalR connected');
  };

  const stopConnection = async () => {
    if (hubConnection) {
      await hubConnection.stop();
      hubConnection = null;
    }
  };

  const sendMessageToHub = async (content: string) => {
    if (!hubConnection) return;
    await hubConnection.invoke('SendMessage', 'Me', content);
  };

  return { connection: hubConnection, startConnection, stopConnection, sendMessageToHub };
}
