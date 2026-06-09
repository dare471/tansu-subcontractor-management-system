import { createI18n } from 'vue-i18n';

const messages = {
  ru: {
    nav: { dashboard: 'Главная', quiz: 'Опрос ТБ', visits: 'Проходы', profile: 'Профиль' },
    dashboard: { title: 'Мой допуск', offlineQr: 'QR (офлайн)', refresh: 'Обновить' },
    locale: { label: 'Язык' }
  },
  kk: {
    nav: { dashboard: 'Басты', quiz: 'ҚТ сауалнамасы', visits: 'Өтулер', profile: 'Профиль' },
    dashboard: { title: 'Менің рұқсатым', offlineQr: 'QR (офлайн)', refresh: 'Жаңарту' },
    locale: { label: 'Тіл' }
  },
  en: {
    nav: { dashboard: 'Home', quiz: 'Safety quiz', visits: 'Visits', profile: 'Profile' },
    dashboard: { title: 'My access pass', offlineQr: 'QR (offline)', refresh: 'Refresh' },
    locale: { label: 'Language' }
  }
};

export const i18n = createI18n({
  legacy: false,
  locale: localStorage.getItem('portal_locale') ?? 'ru',
  fallbackLocale: 'ru',
  messages
});

export function setLocale(locale: 'ru' | 'kk' | 'en') {
  i18n.global.locale.value = locale;
  localStorage.setItem('portal_locale', locale);
}
