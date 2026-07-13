import { createApp } from 'vue';
import App from './App.vue';
import './style.css';
import 'bootstrap/dist/css/bootstrap.min.css';
// @ts-ignore
import 'bootstrap/dist/js/bootstrap.bundle.min';
import { createPinia } from 'pinia';
import router from './router';

const app = createApp(App);
app.use(createPinia());
app.use(router);
app.mount('#app');