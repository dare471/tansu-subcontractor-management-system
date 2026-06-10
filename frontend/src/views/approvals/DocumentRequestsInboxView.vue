<script setup lang="ts">
import { ref, onMounted, h } from 'vue';
import {
  NCard, NSpace, NButton, NDataTable, NInput, NAlert, NTag,
  useMessage, type DataTableColumns
} from 'naive-ui';
import {
  documentRequestsApi, requestTypeLabel, approverRoleLabel,
  type DocumentRequestInboxItem
} from '@/api/documentRequests';
import { toApiError } from '@/api/client';
import AppDrawer from '@/components/AppDrawer.vue';

const msg = useMessage();
const items = ref<DocumentRequestInboxItem[]>([]);
const loading = ref(false);

const decisionItem = ref<DocumentRequestInboxItem | null>(null);
const decisionMode = ref<'approve' | 'reject'>('approve');
const comment = ref('');
const submitting = ref(false);

async function load() {
  loading.value = true;
  try { items.value = await documentRequestsApi.inbox(); }
  catch (e) { msg.error(toApiError(e).detail); }
  finally { loading.value = false; }
}

function openApprove(item: DocumentRequestInboxItem) {
  decisionItem.value = item;
  decisionMode.value = 'approve';
  comment.value = '';
}

function openReject(item: DocumentRequestInboxItem) {
  decisionItem.value = item;
  decisionMode.value = 'reject';
  comment.value = '';
}

async function confirm() {
  if (!decisionItem.value) return;
  if (decisionMode.value === 'reject' && comment.value.trim().length < 3) {
    msg.warning('Комментарий обязателен (мин. 3 символа).');
    return;
  }
  submitting.value = true;
  try {
    if (decisionMode.value === 'approve') {
      await documentRequestsApi.approve(decisionItem.value.sheetId, comment.value.trim() || undefined);
      msg.success('Согласовано');
    } else {
      await documentRequestsApi.reject(decisionItem.value.sheetId, comment.value.trim());
      msg.success('Отклонено');
    }
    decisionItem.value = null;
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { submitting.value = false; }
}

function slaTag(row: DocumentRequestInboxItem) {
  if (row.pendingDays == null) return null;
  const type = row.isEscalated ? 'error' : row.pendingDays >= 3 ? 'warning' : 'default';
  const label = row.isEscalated
    ? `Эскалация · ${row.pendingDays} дн.`
    : `${row.pendingDays} дн. в ожидании`;
  return h(NTag, { size: 'small', type }, () => label);
}

const TABLE_SCROLL_X = 1460;

const columns: DataTableColumns<DocumentRequestInboxItem> = [
  { title: 'Тип', key: 'requestType', width: 140, render: (r) => requestTypeLabel(r.requestType) },
  {
    title: 'Тема', key: 'title', width: 280,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Субподрядчик', key: 'subcontractorName', width: 220,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Проект', key: 'projectName', width: 220,
    ellipsis: { tooltip: true }
  },
  { title: 'Ваша роль', key: 'approverRole', width: 130, render: (r) => approverRoleLabel(r.approverRole) },
  { title: 'Шаг', key: 'orderNo', width: 70, align: 'center' },
  {
    title: 'SLA', key: 'pendingDays', width: 150,
    render: (r) => slaTag(r) ?? '—'
  },
  {
    title: 'Действия', key: 'actions', width: 260,
    render: (row) => h(NSpace, { size: 'small', wrap: false }, () => [
      h(NButton, {
        size: 'small', type: 'success', disabled: !row.canAct,
        onClick: () => openApprove(row)
      }, () => 'Согласовать'),
      h(NButton, {
        size: 'small', type: 'error', disabled: !row.canAct,
        onClick: () => openReject(row)
      }, () => 'Отклонить')
    ])
  }
];

onMounted(load);
</script>

<template>
  <NCard title="Входящие заявки">
    <NSpace vertical>
      <NButton @click="load">Обновить</NButton>
      <div class="t-table-wrap">
        <NDataTable
          class="t-data-table"
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(r) => r.sheetId"
          :scroll-x="TABLE_SCROLL_X"
          size="small"
        />
      </div>
    </NSpace>

    <AppDrawer
      :show="!!decisionItem"
      :title="decisionMode === 'approve' ? 'Согласовать заявку' : 'Отклонить заявку'"
      width="medium"
      @update:show="(v) => { if (!v) decisionItem = null }"
    >
      <NSpace vertical :size="14">
        <NAlert v-if="decisionMode === 'reject'" type="warning">Комментарий обязателен при отклонении.</NAlert>
        <div v-if="decisionItem">
          <b>Тип:</b> {{ requestTypeLabel(decisionItem.requestType) }}<br />
          <b>Тема:</b> {{ decisionItem.title }}<br />
          <b>Субподрядчик:</b> {{ decisionItem.subcontractorName }}<br />
          <b>Роль:</b> {{ approverRoleLabel(decisionItem.approverRole) }}
        </div>
        <NInput
          v-model:value="comment"
          type="textarea"
          :rows="4"
          :placeholder="decisionMode === 'reject' ? 'Причина отклонения' : 'Комментарий (необязательно)'"
        />
        <NSpace justify="end">
          <NButton @click="decisionItem = null">Отмена</NButton>
          <NButton
            :type="decisionMode === 'approve' ? 'success' : 'error'"
            :loading="submitting"
            :disabled="decisionMode === 'reject' && comment.trim().length < 3"
            @click="confirm"
          >
            {{ decisionMode === 'approve' ? 'Согласовать' : 'Отклонить' }}
          </NButton>
        </NSpace>
      </NSpace>
    </AppDrawer>
  </NCard>
</template>
