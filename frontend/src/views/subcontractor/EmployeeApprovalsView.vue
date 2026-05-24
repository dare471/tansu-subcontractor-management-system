<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { NCard, NSpace, NButton, NTag, NTimeline, NTimelineItem, NH3, NText, NEmpty, useMessage } from 'naive-ui';
import { employeesApi } from '@/api/employees';
import { toApiError } from '@/api/client';

type HistoryRow = {
  sheetId: string;
  roundId: string;
  orderNo: number;
  approverUserId: string;
  approverFullName: string;
  status: string;
  comment: string | null;
  decidedAt: string | null;
  createdAt: string;
};

type RoundDto = { roundId: string; overallStatus: string; steps: HistoryRow[] };
type EmployeeApprovalsDto = { employeeId: string; currentStatus: string; rounds: RoundDto[] };

const route = useRoute();
const router = useRouter();
const msg = useMessage();
const data = ref<EmployeeApprovalsDto | null>(null);
const loading = ref(false);

async function load() {
  loading.value = true;
  try {
    data.value = await employeesApi.approvals(route.params.id as string) as EmployeeApprovalsDto;
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { loading.value = false; }
}

function statusType(s: string) {
  if (s === 'approved') return 'success';
  if (s === 'rejected') return 'error';
  if (s === 'pending') return 'warning';
  return 'default';
}

function statusLabel(s: string) {
  return s === 'approved' ? 'Согласовано'
    : s === 'rejected' ? 'Отклонено'
    : s === 'pending' ? 'На согласовании'
    : s === 'skipped' ? 'Пропущено'
    : s;
}

onMounted(load);
</script>

<template>
  <NCard>
    <NSpace vertical>
      <NSpace justify="space-between" align="center">
        <NH3 style="margin:0">История согласования</NH3>
        <NButton @click="router.back()">Назад</NButton>
      </NSpace>
      <NEmpty v-if="data && data.rounds.length === 0" description="Сотрудник ещё не отправлялся на согласование" />
      <template v-for="(round, idx) in data?.rounds ?? []" :key="round.roundId">
        <NCard size="small" :title="`Цикл №${idx + 1}`" :bordered="true">
          <template #header-extra>
            <NTag :type="statusType(round.overallStatus)">{{ statusLabel(round.overallStatus) }}</NTag>
          </template>
          <NTimeline>
            <NTimelineItem
              v-for="step in round.steps"
              :key="step.sheetId"
              :type="statusType(step.status) as any"
              :title="`Шаг ${step.orderNo}: ${step.approverFullName}`"
              :time="step.decidedAt ?? step.createdAt"
            >
              <NSpace vertical :size="6">
                <NTag :type="statusType(step.status)">{{ statusLabel(step.status) }}</NTag>
                <NText v-if="step.comment" depth="2">«{{ step.comment }}»</NText>
              </NSpace>
            </NTimelineItem>
          </NTimeline>
        </NCard>
      </template>
    </NSpace>
  </NCard>
</template>
