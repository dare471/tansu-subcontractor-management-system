<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { NForm, NFormItem, NInput, NButton, NAlert, useMessage } from 'naive-ui';
import { useAuthStore } from '@/stores/auth';
import { toApiError } from '@/api/client';

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();
const msg = useMessage();

const iin = ref('');
const password = ref('');
const error = ref<string | null>(null);
const submitting = ref(false);

onMounted(async () => {
  if (!auth.token) return;
  try {
    if (!auth.user) await auth.fetchMe();
    router.replace(auth.mustChangePassword ? { name: 'change-password' } : { name: 'dashboard' });
  } catch {
    auth.logout();
  }
});

async function submit() {
  error.value = null;
  submitting.value = true;
  try {
    await auth.login(iin.value.trim(), password.value);
    if (auth.mustChangePassword) {
      router.push({ name: 'change-password', query: { redirect: '/' } });
    } else {
      router.push((route.query.redirect as string) || '/');
    }
  } catch (e) {
    error.value = toApiError(e).detail;
    msg.error(error.value);
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="auth-page">
    <div class="auth-card">
      <h1>Личный кабинет</h1>
      <p class="auth-hint">Вход по ИИН и паролю</p>
      <NAlert v-if="error" type="error" style="margin-bottom:12px">{{ error }}</NAlert>
      <NForm @submit.prevent="submit">
        <NFormItem label="ИИН">
          <NInput
            v-model:value="iin"
            placeholder="12 цифр"
            maxlength="12"
            inputmode="numeric"
            size="large"
          />
        </NFormItem>
        <NFormItem label="Пароль">
          <NInput
            v-model:value="password"
            type="password"
            show-password-on="click"
            size="large"
          />
        </NFormItem>
        <NButton
          type="primary"
          attr-type="submit"
          block
          size="large"
          :loading="submitting"
          style="min-height:var(--tap-min)"
        >
          Войти
        </NButton>
      </NForm>
    </div>
  </div>
</template>
