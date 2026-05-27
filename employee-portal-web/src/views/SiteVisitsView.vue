<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { NTimeline, NTimelineItem, NSkeleton, useMessage } from 'naive-ui';
import PageHeader from '@/components/PageHeader.vue';
import { employeePortalApi, formatDateTime, type EmployeeSiteVisits } from '@/api/employeePortal';
import { toApiError } from '@/api/client';

const msg = useMessage();
const data = ref<EmployeeSiteVisits | null>(null);
const loading = ref(true);

const lastVisitLabel = computed(() =>
  data.value?.lastCheckedInAt ? formatDateTime(data.value.lastCheckedInAt) : '—'
);

async function load() {
  loading.value = true;
  try {
    data.value = await employeePortalApi.siteVisits();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <PageHeader title="Проходы" subtitle="Журнал входов на объект через Face ID" />

  <template v-if="loading">
    <div class="portal-card"><NSkeleton height="160px" :sharp="false" /></div>
  </template>

  <template v-else-if="data">
    <div class="stat-grid" style="margin-bottom:16px">
      <div class="stat-box">
        <div class="stat-box__value">{{ data.totalCount }}</div>
        <div class="stat-box__label">Всего проходов</div>
      </div>
      <div class="stat-box" style="grid-column:span 2">
        <div class="stat-box__value" style="font-size:0.9375rem">{{ lastVisitLabel }}</div>
        <div class="stat-box__label">Последний вход</div>
      </div>
    </div>

    <div v-if="data.visits.length === 0" class="empty-state">
      <div class="empty-state__icon">🚪</div>
      <p>Проходов пока нет</p>
    </div>

    <section v-else class="portal-card">
      <NTimeline>
        <NTimelineItem
          v-for="visit in data.visits"
          :key="visit.id"
          type="success"
          :title="visit.projectName ?? 'Объект'"
          :time="visit.checkedInAt"
        >
          <span style="font-size:0.8125rem;color:var(--color-text-muted)">
            {{ visit.verificationMethod }}
            <template v-if="visit.faceConfidence">
              · {{ Math.round(visit.faceConfidence * 100) }}%
            </template>
          </span>
        </NTimelineItem>
      </NTimeline>
    </section>
  </template>
</template>
