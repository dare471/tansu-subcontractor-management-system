import { createApp } from 'vue';
import App from './App.vue';
import './styles.css';

const brandName = (import.meta.env.VITE_APP_BRAND_NAME as string | undefined)?.trim() || 'Tansu';
document.title = `${brandName} — проверка пропуска`;

createApp(App).mount('#app');
