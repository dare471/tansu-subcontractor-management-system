<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { NForm, NFormItem, NInput, NButton, NSpace, NAlert, useMessage } from 'naive-ui';
import { useAuthStore } from '@/stores/auth';
import { toApiError } from '@/api/client';

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();
const msg = useMessage();

const oldPwd = ref('');
const newPwd = ref('');
const confirm = ref('');
const submitting = ref(false);

const isValid = computed(
  () =>
    newPwd.value.length >= 8 &&
    /[A-Z]/.test(newPwd.value) &&
    /[a-z]/.test(newPwd.value) &&
    /[0-9]/.test(newPwd.value) &&
    newPwd.value === confirm.value &&
    oldPwd.value.length > 0
);

async function submit() {
  if (!isValid.value) {
    msg.warning('Проверьте требования к паролю.');
    return;
  }
  submitting.value = true;
  try {
    await auth.changePassword(oldPwd.value, newPwd.value);
    msg.success('Пароль изменён');
    router.push((route.query.redirect as string) || '/');
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="auth-page">
    <div class="auth-card">
      <h2>Смена пароля</h2>
      <p class="auth-hint">Установите постоянный пароль для входа.</p>
      <NAlert type="warning" style="margin-bottom:16px" :show-icon="false">
        Не менее 8 символов, заглавная, строчная буква и цифра.
      </NAlert>
      <NForm @submit.prevent="submit">
        <NFormItem label="Текущий пароль">
          <NInput v-model:value="oldPwd" type="password" show-password-on="click" size="large" />
        </NFormItem>
        <NFormItem label="Новый пароль">
          <NInput v-model:value="newPwd" type="password" show-password-on="click" size="large" />
        </NFormItem>
        <NFormItem label="Повторите новый">
          <NInput v-model:value="confirm" type="password" show-password-on="click" size="large" />
        </NFormItem>
        <NSpace vertical :size="12">
          <NButton
            type="primary"
            block
            size="large"
            :loading="submitting"
            :disabled="!isValid"
            style="min-height:var(--tap-min)"
            @click="submit"
          >
            Сменить пароль
          </NButton>
          <NButton block quaternary @click="auth.logout(); router.push({ name: 'login' })">
            Выйти
          </NButton>
        </NSpace>
      </NForm>
    </div>
  </div>
</template>
