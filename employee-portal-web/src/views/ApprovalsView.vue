<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { NTag, NTimeline, NTimelineItem, NSkeleton, useMessage } from 'naive-ui';
import PageHeader from '@/components/PageHeader.vue';
import StatusBadge from '@/components/StatusBadge.vue';
import {
  employeePortalApi,
  approvalStatusLabel,
  approvalStatusType,
  type EmployeeApprovals
} from '@/api/employeePortal';
import { toApiError } from '@/api/client';

const msg = useMessage();
const data = ref<EmployeeApprovals | null>(null);
const loading = ref(true);

async function load() {
  loading.value = true;
  try {
    data.value = await employeePortalApi.approvals();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <PageHeader title="Согласование" subtitle="Текущий статус и история раундов" />

  <template v-if="loading">
    <div class="portal-card"><NSkeleton height="200px" :sharp="false" /></div>
  </template>

  <template v-else-if="data">
    <section class="portal-card">
      <p style="margin:0 0 8px;font-size:0.875rem;color:var(--color-text-muted)">Текущий статус</p>
      <StatusBadge :status="data.currentStatus" />
    </section>

    <div v-if="data.rounds.length === 0" class="empty-state">
      <div class="empty-state__icon">📋</div>
      <p>Вы ещё не отправлялись на согласование</p>
    </div>

    <section v-for="(round, idx) in data.rounds" :key="round.roundId" class="portal-card">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;gap:8px;flex-wrap:wrap">
        <h2 class="portal-card__title" style="margin:0">Цикл №{{ idx + 1 }}</h2>
        <NTag :type="approvalStatusType(round.overallStatus)" size="small">
          {{ approvalStatusLabel(round.overallStatus) }}
        </NTag>
      </div>
      <NTimeline>
        <NTimelineItem
          v-for="step in round.steps"
          :key="step.sheetId"
          :type="approvalStatusType(step.status) as any"
          :title="`Шаг ${step.orderNo}: ${step.approverFullName}`"
          :time="step.decidedAt ?? step.createdAt"
        >
          <NTag :type="approvalStatusType(step.status)" size="small">
            {{ approvalStatusLabel(step.status) }}
          </NTag>
          <p v-if="step.comment" style="margin:6px 0 0;font-size:0.875rem;color:var(--color-text-muted)">
            «{{ step.comment }}»
          </p>
        </NTimelineItem>
      </NTimeline>
    </section>
  </template>
</template>
