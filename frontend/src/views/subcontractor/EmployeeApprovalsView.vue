<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  NCard, NSpace, NButton, NTag, NTimeline, NTimelineItem, NH3, NText, NEmpty,
  NAlert, NModal, NForm, NFormItem, NInput, useMessage
} from 'naive-ui';
import {
  employeesApi,
  type EmployeeAccessPass,
  type EmployeePpeSummary,
  type PpeIssuance
} from '@/api/employees';
import { apiClient } from '@/api/client';
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
type SiteVisit = {
  id: string;
  checkedInAt: string;
  faceConfidence: number | null;
  projectName: string | null;
  verificationMethod: string;
};

const route = useRoute();
const router = useRouter();
const msg = useMessage();
const employeeId = route.params.id as string;
const data = ref<EmployeeApprovalsDto | null>(null);
const pass = ref<EmployeeAccessPass | null>(null);
const siteVisits = ref<SiteVisit[]>([]);
const ppe = ref<EmployeePpeSummary | null>(null);
const qrBlobUrl = ref<string | null>(null);
const loading = ref(false);

const showIssueModal = ref(false);
const issueType = ref<'helmet' | 'uniform'>('helmet');
const issueForm = ref({ size: '', inventoryNumber: '', notes: '' });
const issuing = ref(false);

const isApproved = computed(() => data.value?.currentStatus === 'approved');

const issueModalTitle = computed(() =>
  issueType.value === 'helmet' ? 'Выдать каску' : 'Выдать униформу'
);

async function loadQr() {
  if (qrBlobUrl.value) {
    URL.revokeObjectURL(qrBlobUrl.value);
    qrBlobUrl.value = null;
  }
  try {
    const res = await apiClient.get(`/api/employees/${employeeId}/access-pass/qr.png`, {
      responseType: 'blob'
    });
    qrBlobUrl.value = URL.createObjectURL(res.data);
  } catch {
    qrBlobUrl.value = null;
  }
}

async function load() {
  loading.value = true;
  try {
    data.value = await employeesApi.approvals(employeeId) as EmployeeApprovalsDto;
    if (data.value.currentStatus === 'approved') {
      try {
        pass.value = await employeesApi.accessPass(employeeId);
        await loadQr();
        siteVisits.value = await employeesApi.siteVisits(employeeId);
        ppe.value = await employeesApi.ppe(employeeId);
      } catch {
        pass.value = null;
        siteVisits.value = [];
        ppe.value = null;
      }
    } else {
      pass.value = null;
      siteVisits.value = [];
      ppe.value = null;
    }
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

function openIssue(type: 'helmet' | 'uniform') {
  issueType.value = type;
  issueForm.value = { size: '', inventoryNumber: '', notes: '' };
  showIssueModal.value = true;
}

async function submitIssue() {
  issuing.value = true;
  try {
    await employeesApi.issuePpe(
      employeeId,
      issueType.value,
      issueForm.value.size || undefined,
      issueForm.value.inventoryNumber || undefined,
      issueForm.value.notes || undefined
    );
    msg.success(issueType.value === 'helmet' ? 'Каска выдана' : 'Униформа выдана');
    showIssueModal.value = false;
    ppe.value = await employeesApi.ppe(employeeId);
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    issuing.value = false;
  }
}

async function returnPpe(item: PpeIssuance) {
  try {
    await employeesApi.returnPpe(employeeId, item.id);
    msg.success('Возврат оформлен');
    ppe.value = await employeesApi.ppe(employeeId);
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
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

      <NCard v-if="isApproved" title="СИЗ — каска и униформа" size="small" :bordered="true">
        <NSpace vertical :size="12">
          <NSpace wrap>
            <NTag :type="ppe?.hasHelmet ? 'success' : 'warning'">
              {{ ppe?.hasHelmet ? '✓ Каска выдана' : 'Каска не выдана' }}
            </NTag>
            <NTag :type="ppe?.hasUniform ? 'success' : 'warning'">
              {{ ppe?.hasUniform ? '✓ Униформа выдана' : 'Униформа не выдана' }}
            </NTag>
          </NSpace>
          <NSpace wrap>
            <NButton size="small" type="primary" @click="openIssue('helmet')">
              {{ ppe?.hasHelmet ? 'Перевыдать каску' : 'Выдать каску' }}
            </NButton>
            <NButton size="small" type="primary" @click="openIssue('uniform')">
              {{ ppe?.hasUniform ? 'Перевыдать униформу' : 'Выдать униформу' }}
            </NButton>
          </NSpace>
          <template v-if="ppe?.activeHelmet">
            <NText depth="3">
              Каска: {{ new Date(ppe.activeHelmet.issuedAt).toLocaleString('ru-RU') }}
              <template v-if="ppe.activeHelmet.size"> · р. {{ ppe.activeHelmet.size }}</template>
            </NText>
            <NButton size="tiny" @click="returnPpe(ppe.activeHelmet!)">Оформить возврат каски</NButton>
          </template>
          <template v-if="ppe?.activeUniform">
            <NText depth="3">
              Униформа: {{ new Date(ppe.activeUniform.issuedAt).toLocaleString('ru-RU') }}
              <template v-if="ppe.activeUniform.size"> · р. {{ ppe.activeUniform.size }}</template>
            </NText>
            <NButton size="tiny" @click="returnPpe(ppe.activeUniform!)">Оформить возврат униформы</NButton>
          </template>
        </NSpace>
      </NCard>

      <NCard v-if="isApproved && pass" title="QR-пропуск" size="small" :bordered="true">
        <NSpace align="center" :size="20">
          <img v-if="qrBlobUrl" :src="qrBlobUrl" alt="QR пропуск" width="160" height="160" />
          <NSpace vertical :size="8">
            <NText depth="3">Выдан: {{ new Date(pass.issuedAt).toLocaleString('ru-RU') }}</NText>
            <NText depth="3">Проверка: {{ pass.verifyUrl }}</NText>
            <NAlert v-if="!pass.hasReferencePhoto" type="warning" :show-icon="false">
              Загрузите фото сотрудника — без него Face ID на проходной не сработает.
            </NAlert>
          </NSpace>
        </NSpace>
      </NCard>

      <NCard v-if="siteVisits.length" title="На объекте" size="small" :bordered="true">
        <NTimeline>
          <NTimelineItem
            v-for="visit in siteVisits"
            :key="visit.id"
            type="success"
            :title="visit.projectName ?? 'Объект'"
            :time="visit.checkedInAt"
          >
            <NSpace vertical :size="4">
              <NText depth="3">
                Face ID
                <template v-if="visit.faceConfidence">
                  · {{ Math.round(visit.faceConfidence * 100) }}%
                </template>
              </NText>
            </NSpace>
          </NTimelineItem>
        </NTimeline>
      </NCard>

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

    <NModal v-model:show="showIssueModal" preset="card" :title="issueModalTitle" style="max-width:420px">
      <NForm @submit.prevent="submitIssue">
        <NFormItem label="Размер">
          <NInput v-model:value="issueForm.size" placeholder="Необязательно" />
        </NFormItem>
        <NFormItem label="Инвентарный №">
          <NInput v-model:value="issueForm.inventoryNumber" placeholder="Необязательно" />
        </NFormItem>
        <NFormItem label="Примечание">
          <NInput v-model:value="issueForm.notes" type="textarea" placeholder="Необязательно" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showIssueModal = false">Отмена</NButton>
          <NButton type="primary" :loading="issuing" @click="submitIssue">Выдать</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </NCard>
</template>
