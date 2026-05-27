<script setup lang="ts">
import { NConfigProvider, NMessageProvider, type GlobalThemeOverrides } from 'naive-ui';

const themeOverrides: GlobalThemeOverrides = {
  common: {
    primaryColor: '#1565c0',
    primaryColorHover: '#1976d2',
    primaryColorPressed: '#0d47a1',
    borderRadius: '8px',
    fontFamily: "system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"
  },
  Button: {
    heightMedium: '44px',
    heightLarge: '48px'
  }
};
</script>

<template>
  <NConfigProvider :theme-overrides="themeOverrides">
    <NMessageProvider>
      <router-view />
    </NMessageProvider>
  </NConfigProvider>
</template>
