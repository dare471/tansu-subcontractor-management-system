<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { NForm, NFormItem, NInput, NButton, NSpace, NAlert, NIcon, useMessage } from 'naive-ui';
import { KeyOutline } from '@vicons/ionicons5';
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
    msg.warning('Проверьте требования к паролю и совпадение полей.');
    return;
  }
  submitting.value = true;
  try {
    await auth.changePassword(oldPwd.value, newPwd.value);
    msg.success('Пароль изменён');
    const redirect = (route.query.redirect as string) || '/';
    router.push(redirect);
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { submitting.value = false; }
}
</script>

<template>
  <div class="t-pwd-bg">
    <div class="t-pwd-card">
      <div style="display:flex;align-items:center;gap:14px;margin-bottom:18px">
        <div style="width:48px;height:48px;border-radius:12px;background:var(--brand-orange-soft);color:var(--brand-orange);display:flex;align-items:center;justify-content:center">
          <NIcon :component="KeyOutline" size="24" />
        </div>
        <div>
          <h2 style="margin:0">Смена пароля</h2>
          <p style="margin:2px 0 0 0;color:var(--brand-text-muted);font-size:13px">
            Установите постоянный пароль для входа в систему.
          </p>
        </div>
      </div>

      <NAlert type="warning" style="margin-bottom:18px">
        Пароль должен содержать не менее 8 символов, заглавную, строчную букву и цифру.
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
        <NSpace justify="space-between" style="margin-top:8px">
          <NButton @click="auth.logout(); router.push({ name: 'login' })">Выйти</NButton>
          <NButton type="primary" size="large" :loading="submitting" :disabled="!isValid" @click="submit">
            Сменить пароль
          </NButton>
        </NSpace>
      </NForm>
    </div>
  </div>
</template>

<style scoped>
.t-pwd-bg {
  min-height: 100vh;
  background:
    radial-gradient(circle at 20% 30%, rgba(238,108,28,0.12), transparent 50%),
    var(--brand-bg);
  display: flex; align-items: center; justify-content: center; padding: 24px;
}
.t-pwd-card {
  width: 520px; max-width: 100%;
  background: #fff;
  border-radius: 16px;
  padding: 28px 32px;
  box-shadow: 0 20px 60px rgba(15,23,42,0.12);
}
</style>
