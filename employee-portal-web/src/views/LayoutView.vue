<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { NButton, NSelect } from 'naive-ui';
import { useAuthStore } from '@/stores/auth';
import { useI18n } from 'vue-i18n';
import { setLocale } from '@/i18n';

const auth = useAuthStore();
const { locale } = useI18n();
const localeOptions = [
  { label: 'Русский', value: 'ru' },
  { label: 'Қазақша', value: 'kk' },
  { label: 'English', value: 'en' }
];
function onLocale(v: 'ru' | 'kk' | 'en') {
  setLocale(v);
}
const router = useRouter();
const route = useRoute();

type NavItem = { key: string; label: string; icon: string; short: string };

const navItems: NavItem[] = [
  { key: 'dashboard', label: 'Главная', icon: '🏠', short: 'Домой' },
  { key: 'approvals', label: 'Согласование', icon: '📋', short: 'Статус' },
  { key: 'site-visits', label: 'Проходы', icon: '🚪', short: 'Входы' },
  { key: 'ppe', label: 'СИЗ', icon: '🦺', short: 'СИЗ' },
  { key: 'profile', label: 'Профиль', icon: '👤', short: 'Профиль' }
];

const activeKey = computed(() => {
  const name = route.name;
  if (typeof name === 'string' && navItems.some((i) => i.key === name)) return name;
  return 'dashboard';
});

function go(key: string) {
  if (route.name !== key) router.push({ name: key });
}

function logout() {
  auth.logout();
  router.push({ name: 'login' });
}
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <span class="app-header__brand">Tansu</span>
      <div class="app-header__user">
        <span class="app-header__name" :title="auth.user?.fullName">{{ auth.user?.fullName }}</span>
        <NSelect
          size="small"
          :value="locale"
          :options="localeOptions"
          style="width: 110px"
          @update:value="onLocale"
        />
        <NButton size="small" quaternary @click="logout">Выйти</NButton>
      </div>
    </header>

    <main class="app-main">
      <nav class="desktop-nav" aria-label="Основное меню">
        <button
          v-for="item in navItems"
          :key="item.key"
          type="button"
          class="desktop-nav__item"
          :class="{ 'desktop-nav__item--active': activeKey === item.key }"
          @click="go(item.key)"
        >
          {{ item.label }}
        </button>
      </nav>

      <router-view />
    </main>

    <nav class="bottom-nav" aria-label="Мобильное меню">
      <button
        v-for="item in navItems"
        :key="item.key"
        type="button"
        class="bottom-nav__item"
        :class="{ 'bottom-nav__item--active': activeKey === item.key }"
        :aria-current="activeKey === item.key ? 'page' : undefined"
        @click="go(item.key)"
      >
        <span class="bottom-nav__icon" aria-hidden="true">{{ item.icon }}</span>
        <span>{{ item.short }}</span>
      </button>
    </nav>
  </div>
</template>
