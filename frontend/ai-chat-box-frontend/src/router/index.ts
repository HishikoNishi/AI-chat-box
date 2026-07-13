import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import ChatBox from '@/components/ChatBox.vue';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'Chat',
    component: ChatBox,
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

export default router;
