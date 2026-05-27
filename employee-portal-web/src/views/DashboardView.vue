<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { NButton, NAlert, NSkeleton, useMessage } from 'naive-ui';
import PageHeader from '@/components/PageHeader.vue';
import StatusBadge from '@/components/StatusBadge.vue';
import {
  employeePortalApi,
  formatDateTime,
  type EmployeePortalDashboard
} from '@/api/employeePortal';
import { toApiError } from '@/api/client';

const router = useRouter();
const msg = useMessage();
const data = ref<EmployeePortalDashboard | null>(null);
const qrUrl = ref<string | null>(null);
const loading = ref(true);
const qrFullscreen = ref(false);

const stepIndex = computed(() => {
  if (data.value?.canShowQrPass) return 3;
  if (data.value?.safetyQuizCompleted) return 2;
  return 1;
});

async function load() {
  loading.value = true;
  try {
    data.value = await employeePortalApi.dashboard();
    if (qrUrl.value) URL.revokeObjectURL(qrUrl.value);
    qrUrl.value = data.value.canShowQrPass ? await employeePortalApi.qrBlob() : null;
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
onUnmounted(() => {
  if (qrUrl.value) URL.revokeObjectURL(qrUrl.value);
});
</script>

<template>
  <PageHeader title="Мой допуск" subtitle="Статус допуска на объект и QR-пропуск" />

  <template v-if="loading">
    <div class="portal-card"><NSkeleton height="120px" :sharp="false" /></div>
    <div class="portal-card"><NSkeleton height="80px" :sharp="false" /></div>
  </template>

  <template v-else-if="data">
    <div class="step-track" role="list" aria-label="Этапы допуска">
      <div
        class="step-pill"
        :class="{ 'step-pill--done': stepIndex > 1, 'step-pill--current': stepIndex === 1 }"
        role="listitem"
      >
        <span class="step-pill__label">1. Инфо</span>
        Объект
      </div>
      <div
        class="step-pill"
        :class="{ 'step-pill--done': stepIndex > 2, 'step-pill--current': stepIndex === 2 }"
        role="listitem"
      >
        <span class="step-pill__label">2. ТБ</span>
        Опрос
      </div>
      <div
        class="step-pill"
        :class="{ 'step-pill--done': stepIndex >= 3, 'step-pill--current': stepIndex === 3 }"
        role="listitem"
      >
        <span class="step-pill__label">3. QR</span>
        Пропуск
      </div>
    </div>

    <section class="portal-card">
      <div style="display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin-bottom:12px">
        <StatusBadge :status="data.approvalStatus" />
        <span
          v-if="data.hasHelmet && data.hasUniform"
          style="font-size:0.8125rem;color:var(--color-success)"
        >✓ СИЗ выдано</span>
        <span
          v-else
          style="font-size:0.8125rem;color:var(--color-warning)"
        >⚠ СИЗ неполное</span>
      </div>
      <p style="margin:0 0 4px;font-weight:600">{{ data.fullName }}</p>
      <p style="margin:0 0 12px;font-size:0.875rem;color:var(--color-text-muted)">
        {{ data.position }} · {{ data.subcontractorName }}
      </p>
      <p style="margin:0;font-size:0.875rem">
        <strong>Объект:</strong> {{ data.projectName ?? 'не указан' }}
      </p>
    </section>

    <section v-if="!data.isApproved" class="portal-card">
      <NAlert type="warning" :show-icon="false" title="Ожидается согласование">
        QR-пропуск откроется после полного согласования.
      </NAlert>
      <NButton type="primary" block style="margin-top:12px" @click="router.push({ name: 'approvals' })">
        Смотреть статус
      </NButton>
    </section>

    <section v-if="data.isApproved && !data.safetyQuizCompleted" class="portal-card">
      <h2 class="portal-card__title">Опрос по ТБ</h2>
      <p style="margin:0 0 12px;font-size:0.875rem;color:var(--color-text-muted)">
        Короткий тест перед получением QR-пропуска.
      </p>
      <NButton type="primary" block @click="router.push({ name: 'quiz' })">Пройти опрос</NButton>
    </section>

    <section
      v-if="(!data.hasHelmet || !data.hasUniform) && data.isApproved"
      class="portal-card"
    >
      <h2 class="portal-card__title">СИЗ</h2>
      <p style="margin:0 0 12px;font-size:0.875rem;color:var(--color-text-muted)">
        {{ !data.hasHelmet && !data.hasUniform
          ? 'Каска и униформа ещё не выданы — обратитесь на пункт выдачи.'
          : !data.hasHelmet ? 'Каска не выдана.' : 'Униформа не выдана.' }}
      </p>
      <NButton block @click="router.push({ name: 'ppe' })">Подробнее о СИЗ</NButton>
    </section>

    <section v-if="data.canShowQrPass" class="portal-card">
      <h2 class="portal-card__title">QR-пропуск</h2>
      <div class="qr-display">
        <img v-if="qrUrl" :src="qrUrl" alt="QR-код пропуска" />
        <p v-if="data.accessPass" style="margin:0;font-size:0.8125rem;color:var(--color-text-muted);text-align:center">
          Выдан {{ formatDateTime(data.accessPass.issuedAt) }}
        </p>
        <NButton v-if="qrUrl" type="primary" block @click="qrFullscreen = true">
          Показать на весь экран
        </NButton>
      </div>
      <NAlert
        v-if="data.accessPass && !data.accessPass.hasReferencePhoto"
        type="warning"
        :show-icon="false"
        style="margin-top:12px"
      >
        Нет фото для Face ID —
        <NButton text type="primary" @click="router.push({ name: 'profile' })">загрузите в профиле</NButton>
      </NAlert>
    </section>

    <div v-if="qrFullscreen && qrUrl" class="qr-fullscreen" @click="qrFullscreen = false">
      <img :src="qrUrl" alt="QR-код" @click.stop />
      <p style="margin-top:16px;font-size:0.875rem;color:var(--color-text-muted)">Нажмите, чтобы закрыть</p>
    </div>
  </template>
</template>
