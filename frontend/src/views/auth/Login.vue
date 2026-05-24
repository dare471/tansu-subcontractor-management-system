<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { NForm, NFormItem, NInput, NButton, NSpace, NAlert, NIcon, useMessage } from 'naive-ui';
import { MailOutline, LockClosedOutline } from '@vicons/ionicons5';
import { useAuthStore } from '@/stores/auth';
import { toApiError } from '@/api/client';

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();
const msg = useMessage();

const email = ref('');
const password = ref('');
const devEmail = ref('admin@tansu.local');
const error = ref<string | null>(null);
const submitting = ref(false);
const isDev = import.meta.env.DEV;

async function submit() {
  error.value = null;
  submitting.value = true;
  try {
    await auth.login(email.value.trim(), password.value);
    const redirect = (route.query.redirect as string) || '/';
    if (auth.mustChangePassword) router.push({ name: 'change-password' });
    else router.push(redirect);
  } catch (e) {
    const err = toApiError(e);
    error.value = err.detail;
    msg.error(err.detail);
  } finally { submitting.value = false; }
}

async function devLogin() {
  error.value = null;
  submitting.value = true;
  try {
    await auth.devLogin(devEmail.value.trim());
    router.push((route.query.redirect as string) || '/');
  } catch (e) {
    const err = toApiError(e);
    error.value = err.detail;
    msg.error(err.detail);
  } finally { submitting.value = false; }
}
</script>

<template>
  <div class="t-login-bg">
    <div class="t-login-card">
      <div class="t-login-brand">
        <div class="t-login-logo">T</div>
        <div>
          <div style="font-size:22px;font-weight:800;color:#fff;letter-spacing:0.5px">TANSU</div>
          <div style="color:rgba(255,255,255,.6);font-size:12px;text-transform:uppercase;letter-spacing:1px">Субподрядчики</div>
        </div>
      </div>

      <div class="t-login-form">
        <h2 style="margin:0 0 6px 0;font-size:22px">Вход в систему</h2>
        <p style="color:var(--brand-text-muted);margin:0 0 22px 0;font-size:14px">
          Используйте email и пароль, выданный администратором.
        </p>

        <NAlert v-if="error" type="error" style="margin-bottom:14px">{{ error }}</NAlert>

        <NForm @submit.prevent="submit">
          <NFormItem label="Email">
            <NInput v-model:value="email" placeholder="email@company.kz" autocomplete="username" size="large">
              <template #prefix><NIcon :component="MailOutline" /></template>
            </NInput>
          </NFormItem>
          <NFormItem label="Пароль">
            <NInput
              v-model:value="password"
              type="password"
              show-password-on="click"
              autocomplete="current-password"
              size="large"
            >
              <template #prefix><NIcon :component="LockClosedOutline" /></template>
            </NInput>
          </NFormItem>
          <NButton type="primary" block size="large" :loading="submitting" attr-type="submit" @click="submit">
            Войти
          </NButton>
        </NForm>

        <template v-if="isDev">
          <div style="margin-top:24px;padding-top:20px;border-top:1px dashed var(--brand-border)">
            <p style="margin:0 0 14px 0;font-size:13px;color:var(--brand-text-muted)">Вход для сотрудников ТАНСУ</p>
            <NForm @submit.prevent="devLogin">
              <NFormItem label="Email">
                <NInput v-model:value="devEmail" placeholder="admin@tansu.local" size="large">
                  <template #prefix><NIcon :component="MailOutline" /></template>
                </NInput>
              </NFormItem>
              <NButton
                block
                size="large"
                secondary
                :loading="submitting"
                @click="devLogin"
              >
                Войти
              </NButton>
            </NForm>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.t-login-bg {
  min-height: 100vh;
  display: flex; align-items: center; justify-content: center;
  background:
    radial-gradient(circle at 20% 30%, rgba(238,108,28,0.12), transparent 50%),
    radial-gradient(circle at 80% 70%, rgba(31,42,68,0.08), transparent 50%),
    var(--brand-bg);
  padding: 24px;
}
.t-login-card {
  width: 880px;
  max-width: 100%;
  display: grid;
  grid-template-columns: 340px 1fr;
  background: #fff;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 20px 60px rgba(15,23,42,0.12);
}
.t-login-brand {
  background: linear-gradient(160deg, var(--brand-navy) 0%, var(--brand-navy-dark) 100%);
  padding: 36px 28px;
  display: flex; flex-direction: column; gap: 16px;
}
.t-login-logo {
  width: 56px; height: 56px;
  border-radius: 14px;
  background: var(--brand-orange);
  color: #fff; font-weight: 800; font-size: 28px;
  display: flex; align-items: center; justify-content: center;
}
.t-login-form { padding: 40px 36px; }

@media (max-width: 720px) {
  .t-login-card { grid-template-columns: 1fr; }
}
</style>
