<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { NButton, NUpload, NAlert, NSkeleton, useMessage, type UploadFileInfo } from 'naive-ui';
import PageHeader from '@/components/PageHeader.vue';
import StatusBadge from '@/components/StatusBadge.vue';
import {
  employeePortalApi,
  formatDateTime,
  type EmployeePortalProfile
} from '@/api/employeePortal';
import { toApiError } from '@/api/client';

const router = useRouter();
const msg = useMessage();
const profile = ref<EmployeePortalProfile | null>(null);
const photoUrl = ref<string | null>(null);
const loading = ref(true);
const uploading = ref(false);

async function loadPhoto() {
  if (photoUrl.value) {
    URL.revokeObjectURL(photoUrl.value);
    photoUrl.value = null;
  }
  if (profile.value?.hasPhoto) {
    photoUrl.value = await employeePortalApi.photoBlob();
  }
}

async function load() {
  loading.value = true;
  try {
    profile.value = await employeePortalApi.profile();
    await loadPhoto();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

async function onPhotoChange(options: { file: UploadFileInfo }) {
  const raw = options.file.file;
  if (!raw) return;
  uploading.value = true;
  try {
    const result = await employeePortalApi.uploadPhoto(raw);
    if (result.status === 'approved') msg.success(result.message);
    else if (result.status === 'pending') msg.warning(result.message);
    else msg.error(result.message);
    await load();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    uploading.value = false;
  }
}

onMounted(load);
onUnmounted(() => {
  if (photoUrl.value) URL.revokeObjectURL(photoUrl.value);
});
</script>

<template>
  <PageHeader title="Профиль" subtitle="Личные данные и настройки" />

  <template v-if="loading">
    <div class="portal-card"><NSkeleton height="200px" :sharp="false" /></div>
  </template>

  <template v-else-if="profile">
    <section class="portal-card">
      <div style="display:flex;gap:16px;align-items:flex-start;flex-wrap:wrap">
        <div class="avatar">
          <img v-if="photoUrl" :src="photoUrl" alt="Фото" />
          <span v-else class="avatar__placeholder">👤</span>
        </div>
        <div style="flex:1;min-width:180px">
          <p style="margin:0 0 4px;font-weight:600;font-size:1.0625rem">{{ profile.fullName }}</p>
          <p style="margin:0 0 8px;color:var(--color-text-muted);font-size:0.875rem">{{ profile.position }}</p>
          <StatusBadge :status="profile.approvalStatus" />
        </div>
      </div>
    </section>

    <section class="portal-card">
      <h2 class="portal-card__title">Контакты</h2>
      <dl class="info-list">
        <div class="info-list__row"><dt>ИИН</dt><dd>{{ profile.iin }}</dd></div>
        <div class="info-list__row"><dt>Телефон</dt><dd>{{ profile.phone || '—' }}</dd></div>
        <div class="info-list__row"><dt>Субподрядчик</dt><dd>{{ profile.subcontractorName }}</dd></div>
        <div class="info-list__row"><dt>Объект</dt><dd>{{ profile.projectName ?? '—' }}</dd></div>
        <div v-if="profile.accessPassIssuedAt" class="info-list__row">
          <dt>Пропуск</dt>
          <dd>{{ formatDateTime(profile.accessPassIssuedAt) }}</dd>
        </div>
      </dl>
    </section>

    <section class="portal-card">
      <h2 class="portal-card__title">Фото для Face ID</h2>
      <NAlert
        v-if="profile.photoReviewStatus === 'rejected'"
        type="error"
        :show-icon="false"
        style="margin-bottom:12px"
      >
        {{ profile.photoReviewReason ?? 'Фото отклонено. Загрузите новое.' }}
      </NAlert>
      <NAlert
        v-else-if="profile.photoReviewStatus === 'pending'"
        type="warning"
        :show-icon="false"
        style="margin-bottom:12px"
      >
        Фото на проверке у ответственного сотрудника ТАНСУ.
      </NAlert>
      <NAlert type="info" :show-icon="false" style="margin-bottom:12px">
        JPEG/JPG, лицо без очков и головных уборов, нейтральный фон. Размер файла — по правилам вашей организации.
        Отправка на согласование возможна только после одобрения фото.
      </NAlert>
      <NUpload accept=".jpg,.jpeg,image/jpeg" :max="1" :show-file-list="false" :disabled="uploading" @change="onPhotoChange">
        <NButton block :loading="uploading" style="min-height:var(--tap-min)">
          {{ profile.hasPhoto ? 'Заменить фото' : 'Загрузить фото' }}
        </NButton>
      </NUpload>
    </section>

    <NButton block quaternary type="primary" style="min-height:var(--tap-min)" @click="router.push({ name: 'change-password' })">
      Сменить пароль
    </NButton>
  </template>
</template>

<style scoped>
.avatar {
  width: 88px;
  height: 88px;
  border-radius: var(--radius-md);
  overflow: hidden;
  background: var(--color-bg);
  flex-shrink: 0;
}
.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.avatar__placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  font-size: 2rem;
}
.info-list {
  margin: 0;
}
.info-list__row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 0;
  border-bottom: 1px solid var(--color-border);
  font-size: 0.875rem;
}
.info-list__row:last-child {
  border-bottom: none;
}
.info-list__row dt {
  color: var(--color-text-muted);
  margin: 0;
}
.info-list__row dd {
  margin: 0;
  text-align: right;
  font-weight: 500;
}
</style>
