<script setup lang="ts">
import { ref, onMounted, h } from 'vue';
import {
  NCard, NSpace, NButton, NDataTable, NInput, NAlert,
  useMessage, type DataTableColumns
} from 'naive-ui';
import { approvalsApi, type InboxItem } from '@/api/approvals';
import { toApiError } from '@/api/client';
import AppDrawer from '@/components/AppDrawer.vue';

const msg = useMessage();
const items = ref<InboxItem[]>([]);
const loading = ref(false);

const decisionItem = ref<InboxItem | null>(null);
const decisionMode = ref<'approve' | 'reject'>('approve');
const comment = ref('');
const submitting = ref(false);

async function load() {
  loading.value = true;
  try { items.value = await approvalsApi.inbox(); }
  catch (e) { msg.error(toApiError(e).detail); }
  finally { loading.value = false; }
}

function openApprove(item: InboxItem) {
  decisionItem.value = item; decisionMode.value = 'approve'; comment.value = '';
}
function openReject(item: InboxItem) {
  decisionItem.value = item; decisionMode.value = 'reject'; comment.value = '';
}

async function confirm() {
  if (!decisionItem.value) return;
  if (decisionMode.value === 'reject' && comment.value.trim().length < 3) {
    msg.warning('Комментарий обязателен и не короче 3 символов.');
    return;
  }
  submitting.value = true;
  try {
    if (decisionMode.value === 'approve') {
      await approvalsApi.approve(decisionItem.value.sheetId, comment.value.trim() || undefined);
      msg.success('Согласовано');
    } else {
      await approvalsApi.reject(decisionItem.value.sheetId, comment.value.trim());
      msg.success('Отклонено');
    }
    decisionItem.value = null;
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { submitting.value = false; }
}

const TABLE_SCROLL_X = 1100;

const columns: DataTableColumns<InboxItem> = [
  {
    title: 'Сотрудник', key: 'employeeFullName', width: 200,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Должность', key: 'position', width: 160,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Субподрядчик', key: 'subcontractorName', width: 180,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Проект', key: 'projectName', width: 200,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Пакет', key: 'batchTitle', width: 180,
    ellipsis: { tooltip: true },
    render: (r) => r.batchTitle ?? '—'
  },
  { title: 'Шаг', key: 'orderNo', width: 72, align: 'center' },
  {
    title: 'Действия', key: 'a', width: 240,
    render: (row) => h(NSpace, { size: 'small' }, () => [
      h(NButton, { size: 'small', type: 'success', onClick: () => openApprove(row) }, () => 'Согласовать'),
      h(NButton, { size: 'small', type: 'error', onClick: () => openReject(row) }, () => 'Отклонить')
    ])
  }
];

onMounted(load);
</script>

<template>
  <NCard title="Входящие согласования">
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
      :title="decisionMode === 'approve' ? 'Согласовать' : 'Отклонить'"
      width="medium"
      @update:show="(v) => { if (!v) decisionItem = null }"
    >
      <NSpace vertical :size="14">
        <NAlert v-if="decisionMode === 'reject'" type="warning">
          Комментарий обязателен для отклонения.
        </NAlert>
        <div>
          <b>Сотрудник:</b> {{ decisionItem?.employeeFullName }}<br />
          <b>Субподрядчик:</b> {{ decisionItem?.subcontractorName }}<br />
          <b>Проект:</b> {{ decisionItem?.projectName }}
        </div>
        <NInput
          v-model:value="comment"
          type="textarea"
          :rows="4"
          :placeholder="decisionMode === 'reject' ? 'Причина отклонения (обязательно)' : 'Комментарий (необязательно)'"
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
