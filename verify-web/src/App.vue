<script setup lang="ts">
import { NConfigProvider, NMessageProvider } from 'naive-ui';
import VerifyPage from './VerifyPage.vue';
</script>

<template>
  <NConfigProvider>
    <NMessageProvider>
      <VerifyPage />
    </NMessageProvider>
  </NConfigProvider>
</template>
