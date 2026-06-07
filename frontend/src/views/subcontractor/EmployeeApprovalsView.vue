<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  NCard, NSpace, NButton, NTag, NTimeline, NTimelineItem, NH3, NText, NEmpty,
  NAlert, NForm, NFormItem, NInput, NSelect, NUpload, NDataTable,
  NPopconfirm, useMessage, type DataTableColumns, type UploadFileInfo
} from 'naive-ui';
import {
  employeesApi,
  type EmployeeAccessPass,
  type EmployeePpeSummary,
  type PpeIssuance,
  type EmployeeDocumentsSummary,
  type EmployeeDocument,
  type EmployeeBlockStatus
} from '@/api/employees';
import { apiClient } from '@/api/client';
import { toApiError } from '@/api/client';
import { useAuthStore } from '@/stores/auth';
import AppDrawer from '@/components/AppDrawer.vue';

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

const DOCUMENT_TYPES = [
  { label: 'Удостоверение личности', value: 'id_card' },
  { label: 'Сертификат / допуск', value: 'certificate' },
  { label: 'Инструктаж по ТБ', value: 'safety_briefing' },
  { label: 'Медицинская справка', value: 'medical' },
  { label: 'Допуск на работы', value: 'permit' },
  { label: 'Иной документ', value: 'other' }
];

const route = useRoute();
const router = useRouter();
const msg = useMessage();
const auth = useAuthStore();
const employeeId = route.params.id as string;

const data = ref<EmployeeApprovalsDto | null>(null);
const pass = ref<EmployeeAccessPass | null>(null);
const siteVisits = ref<SiteVisit[]>([]);
const ppe = ref<EmployeePpeSummary | null>(null);
const documents = ref<EmployeeDocumentsSummary | null>(null);
const blocks = ref<EmployeeBlockStatus | null>(null);
const qrBlobUrl = ref<string | null>(null);
const loading = ref(false);

const showIssueModal = ref(false);
const issueType = ref<'helmet' | 'uniform'>('helmet');
const issueForm = ref({ size: '', inventoryNumber: '', notes: '' });
const issuing = ref(false);

const showBlockModal = ref(false);
const blockReason = ref('');
const blocking = ref(false);

const showDocModal = ref(false);
const docForm = ref({ name: '', documentType: 'id_card', expiresAt: '' });
const replaceDocId = ref<string | null>(null);
const uploadingDoc = ref(false);

const previewUrl = ref<string | null>(null);
const previewContentType = ref<string | null>(null);
const showPreview = ref(false);

const isApproved = computed(() => data.value?.currentStatus === 'approved');
const canBlock = computed(() => auth.canBlockEmployee && isApproved.value && !blocks.value?.isBlocked);
const canUploadDocs = computed(() => auth.isTansu || auth.isSubcontractor);

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
    const [approvals, docs, blockStatus] = await Promise.all([
      employeesApi.approvals(employeeId) as Promise<EmployeeApprovalsDto>,
      employeesApi.documents(employeeId),
      employeesApi.blocks(employeeId)
    ]);
    data.value = approvals;
    documents.value = docs;
    blocks.value = blockStatus;

    if (data.value.currentStatus === 'approved' && !blockStatus.isBlocked) {
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

async function submitBlock() {
  if (blockReason.value.trim().length < 3) {
    msg.warning('Укажите причину блокировки (не короче 3 символов).');
    return;
  }
  blocking.value = true;
  try {
    await employeesApi.block(employeeId, blockReason.value.trim());
    msg.success('Сотрудник заблокирован. Субподрядчик уведомлён.');
    showBlockModal.value = false;
    blockReason.value = '';
    await load();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    blocking.value = false;
  }
}

function openUploadDoc(replace?: EmployeeDocument) {
  replaceDocId.value = replace?.id ?? null;
  docForm.value = {
    name: replace?.name ?? '',
    documentType: replace?.documentType ?? 'id_card',
    expiresAt: ''
  };
  showDocModal.value = true;
}

async function onDocFileChange(options: { file: UploadFileInfo }) {
  const raw = options.file.file;
  if (!raw) return;
  if (!docForm.value.name.trim()) {
    msg.warning('Укажите наименование документа.');
    return;
  }
  uploadingDoc.value = true;
  try {
    await employeesApi.uploadDocument(
      employeeId,
      raw,
      docForm.value.name.trim(),
      docForm.value.documentType,
      docForm.value.expiresAt || undefined,
      replaceDocId.value ?? undefined
    );
    msg.success(replaceDocId.value ? 'Новая версия загружена' : 'Документ загружен');
    showDocModal.value = false;
    documents.value = await employeesApi.documents(employeeId);
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    uploadingDoc.value = false;
  }
}

async function previewDocument(doc: EmployeeDocument) {
  try {
    const res = await apiClient.get(
      `/api/employees/${employeeId}/documents/${doc.id}/file`,
      { responseType: 'blob' }
    );
    if (previewUrl.value) URL.revokeObjectURL(previewUrl.value);
    previewUrl.value = URL.createObjectURL(res.data);
    previewContentType.value = doc.contentType;
    showPreview.value = true;
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

async function removeDocument(doc: EmployeeDocument) {
  try {
    await employeesApi.deleteDocument(employeeId, doc.id);
    msg.success('Документ удалён');
    documents.value = await employeesApi.documents(employeeId);
  } catch (e) {
    msg.error(toApiError(e).detail);
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

function blockActionLabel(action: string) {
  return action === 'block' ? 'Блокировка' : 'Разблокировка';
}

const docColumns: DataTableColumns<EmployeeDocument> = [
  { title: 'Документ', key: 'name', ellipsis: { tooltip: true } },
  { title: 'Тип', key: 'documentTypeLabel', width: 160 },
  {
    title: 'Загружен', key: 'uploadedAt', width: 150,
    render: (r) => new Date(r.uploadedAt).toLocaleDateString('ru-RU')
  },
  {
    title: 'Срок', key: 'expiresAt', width: 120,
    render: (r) => {
      if (!r.expiresAt) return '—';
      if (r.isExpired) return 'Истёк';
      if (r.isExpiringSoon) return '≤ 14 дн.';
      return new Date(r.expiresAt).toLocaleDateString('ru-RU');
    }
  },
  { title: 'Версия', key: 'versionNo', width: 72, align: 'center' },
  {
    title: '', key: 'actions', width: 220,
    render: (row) => h(NSpace, { size: 'small' }, () => [
      h(NButton, { size: 'tiny', onClick: () => previewDocument(row) }, () => 'Просмотр'),
      canUploadDocs.value && !row.isSuperseded
        ? h(NButton, { size: 'tiny', onClick: () => openUploadDoc(row) }, () => 'Новая версия')
        : null,
      canUploadDocs.value && !row.isSuperseded
        ? h(NPopconfirm, { onPositiveClick: () => removeDocument(row) }, {
            default: () => 'Удалить документ?',
            trigger: () => h(NButton, { size: 'tiny', type: 'error' }, () => 'Удалить')
          })
        : null
    ])
  }
];

onMounted(load);
</script>

<template>
  <NCard>
    <NSpace vertical>
      <NSpace justify="space-between" align="center" wrap>
        <NH3 style="margin:0">Карточка сотрудника</NH3>
        <NSpace>
          <NButton v-if="canBlock" type="error" @click="showBlockModal = true">Заблокировать</NButton>
          <NButton @click="router.back()">Назад</NButton>
        </NSpace>
      </NSpace>

      <NAlert v-if="blocks?.isBlocked" type="error" title="Сотрудник заблокирован">
        {{ blocks.lastRecord?.reason }}
        <template #footer>
          Доступ на объект отозван. Для восстановления необходимо повторное согласование.
        </template>
      </NAlert>

      <NCard title="Документы" size="small" :bordered="true">
        <NSpace vertical :size="12">
          <NAlert v-if="documents?.expiringWithin14Days" type="warning" :show-icon="false">
            {{ documents.expiringWithin14Days }} документ(ов) истекает в ближайшие 14 дней.
          </NAlert>
          <NButton v-if="canUploadDocs" size="small" type="primary" @click="openUploadDoc()">
            Загрузить документ
          </NButton>
          <NDataTable
            v-if="documents?.documents.length"
            size="small"
            :columns="docColumns"
            :data="documents.documents"
            :row-key="(r) => r.id"
          />
          <NEmpty v-else description="Документы не загружены" />
        </NSpace>
      </NCard>

      <NCard title="Журнал блокировок и нарушений" size="small" :bordered="true">
        <NEmpty v-if="!blocks?.history.length" description="Записей нет" />
        <NTimeline v-else>
          <NTimelineItem
            v-for="item in blocks.history"
            :key="item.id"
            :type="item.actionType === 'block' ? 'error' : 'success'"
            :title="blockActionLabel(item.actionType)"
            :time="item.createdAt"
          >
            <NSpace vertical :size="4">
              <NText depth="3">
                {{ item.initiatedByFullName }}
                <template v-if="item.initiatorRoleLabel"> · {{ item.initiatorRoleLabel }}</template>
              </NText>
              <NText>{{ item.reason }}</NText>
              <NTag v-if="item.status" size="small" :type="item.status === 'applied' ? 'success' : 'default'">
                {{ item.status === 'applied' ? 'Применено' : item.status }}
              </NTag>
            </NSpace>
          </NTimelineItem>
        </NTimeline>
      </NCard>

      <NCard v-if="isApproved && !blocks?.isBlocked && pass" title="QR-пропуск" size="small" :bordered="true">
        <NSpace align="center" :size="20">
          <img v-if="qrBlobUrl" :src="qrBlobUrl" alt="QR пропуск" width="160" height="160" />
          <NSpace vertical :size="8">
            <NText depth="3">Выдан: {{ new Date(pass.issuedAt).toLocaleString('ru-RU') }}</NText>
            <NAlert v-if="!pass.hasReferencePhoto" type="warning" :show-icon="false">
              Загрузите фото сотрудника — без него Face ID на проходной не сработает.
            </NAlert>
          </NSpace>
        </NSpace>
      </NCard>

      <NCard v-if="isApproved && !blocks?.isBlocked" title="СИЗ — каска и униформа" size="small" :bordered="true">
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
            <NButton size="small" type="primary" @click="openIssue('helmet')">Выдать каску</NButton>
            <NButton size="small" type="primary" @click="openIssue('uniform')">Выдать униформу</NButton>
          </NSpace>
        </NSpace>
      </NCard>

      <NCard v-if="siteVisits.length" title="Проходы на объект" size="small" :bordered="true">
        <NTimeline>
          <NTimelineItem
            v-for="visit in siteVisits"
            :key="visit.id"
            type="success"
            :title="visit.projectName ?? 'Объект'"
            :time="visit.checkedInAt"
          />
        </NTimeline>
      </NCard>

      <NH3 style="margin:8px 0 0">История согласования</NH3>
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
              <NTag :type="statusType(step.status)">{{ statusLabel(step.status) }}</NTag>
              <NText v-if="step.comment" depth="2" style="display:block;margin-top:4px">«{{ step.comment }}»</NText>
            </NTimelineItem>
          </NTimeline>
        </NCard>
      </template>
    </NSpace>

    <AppDrawer v-model:show="showBlockModal" title="Блокировка сотрудника" width="narrow">
      <NAlert type="warning" :show-icon="false" style="margin-bottom:12px">
        Доступ на объект (Hikvision) будет отозван. Субподрядчик получит уведомление с указанной причиной.
      </NAlert>
      <NInput v-model:value="blockReason" type="textarea" :rows="4" placeholder="Причина / нарушение (обязательно)" />
      <NSpace justify="end" style="margin-top:16px">
        <NButton @click="showBlockModal = false">Отмена</NButton>
        <NButton type="error" :loading="blocking" @click="submitBlock">Заблокировать</NButton>
      </NSpace>
    </AppDrawer>

    <AppDrawer v-model:show="showDocModal" :title="replaceDocId ? 'Новая версия документа' : 'Загрузить документ'" width="narrow">
      <NForm @submit.prevent>
        <NFormItem label="Наименование"><NInput v-model:value="docForm.name" /></NFormItem>
        <NFormItem label="Тип"><NSelect v-model:value="docForm.documentType" :options="DOCUMENT_TYPES" /></NFormItem>
        <NFormItem label="Срок действия"><NInput v-model:value="docForm.expiresAt" type="date" /></NFormItem>
        <NFormItem label="Файл (PDF, JPG, PNG)">
          <NUpload accept=".pdf,.jpg,.jpeg,.png" :max="1" :show-file-list="false" :disabled="uploadingDoc" @change="onDocFileChange">
            <NButton :loading="uploadingDoc">Выбрать файл</NButton>
          </NUpload>
        </NFormItem>
      </NForm>
    </AppDrawer>

    <AppDrawer v-model:show="showPreview" title="Просмотр документа" width="full" @after-leave="previewUrl = null">
      <iframe v-if="previewUrl && previewContentType === 'application/pdf'" :src="previewUrl" style="width:100%;height:calc(100vh - 160px);border:none" />
      <img v-else-if="previewUrl" :src="previewUrl" alt="Документ" style="max-width:100%;height:auto" />
    </AppDrawer>

    <AppDrawer v-model:show="showIssueModal" :title="issueType === 'helmet' ? 'Выдать каску' : 'Выдать униформу'" width="narrow">
      <NForm @submit.prevent="submitIssue">
        <NFormItem label="Размер"><NInput v-model:value="issueForm.size" /></NFormItem>
        <NFormItem label="Инвентарный №"><NInput v-model:value="issueForm.inventoryNumber" /></NFormItem>
        <NFormItem label="Примечание"><NInput v-model:value="issueForm.notes" type="textarea" /></NFormItem>
        <NSpace justify="end">
          <NButton @click="showIssueModal = false">Отмена</NButton>
          <NButton type="primary" :loading="issuing" @click="submitIssue">Выдать</NButton>
        </NSpace>
      </NForm>
    </AppDrawer>
  </NCard>
</template>
