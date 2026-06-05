import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import { appBrand } from './config/branding';
import 'vfonts/Lato.css';
import './styles/app.css';

document.title = `${appBrand.brandName} — Субподрядчики`;

const app = createApp(App);
app.use(createPinia());
app.use(router);
app.mount('#app');
