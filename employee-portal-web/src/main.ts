import { createApp } from 'vue';
import { createPinia } from 'pinia';
import naive from 'naive-ui';
import App from './App.vue';
import router from './router';
import { appBrand } from './config/branding';
import './styles.css';
import { i18n } from './i18n';

document.title = `${appBrand.brandName} — личный кабинет`;

const app = createApp(App);
app.use(createPinia());
app.use(i18n);
app.use(router);
app.use(naive);
app.mount('#app');
