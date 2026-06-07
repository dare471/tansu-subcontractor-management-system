<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NForm, NFormItem,
  NSelect, NPopconfirm, NTag, NEllipsis, useMessage, type DataTableColumns
} from 'naive-ui';
import {
  documentRequestsApi, REQUEST_TYPES, requestTypeLabel, approverRoleLabel,
  type DocumentRequest
} from '@/api/documentRequests';
import { authApi, type MyProject } from '@/api/auth';
import { useAuthStore } from '@/stores/auth';
import { toApiError } from '@/api/client';
import AppDrawer from '@/components/AppDrawer.vue';

const msg = useMessage();
const auth = useAuthStore();

const items = ref<DocumentRequest[]>([]);
const loading = ref(false);
const search = ref('');
const filterType = ref<string | null>(null);
const projects = ref<MyProject[]>([]);

const showForm = ref(false);
const editing = ref<DocumentRequest | null>(null);
const form = ref({ projectOid: '', requestType: 'leave', title: '', description: '' });

async function load() {
  loading.value = true;
  try {
    items.value = await documentRequestsApi.list({
      search: search.value || undefined,
      requestType: filterType.value || undefined
    });
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { loading.value = false; }
}

async function loadProjects() {
  if (!auth.user?.subcontractorId) return;
  projects.value = await authApi.myProjects();
}

const projectOptions = computed(() =>
  projects.value.map((p) => ({ label: p.name || p.projectOid, value: p.projectOid }))
);

const typeOptions = REQUEST_TYPES.map((t) => ({ label: t.label, value: t.value }));

function openCreate() {
  editing.value = null;
  form.value = {
    projectOid: projects.value[0]?.projectOid ?? '',
    requestType: 'leave',
    title: '',
    description: ''
  };
  showForm.value = true;
}

function openEdit(row: DocumentRequest) {
  editing.value = row;
  form.value = {
    projectOid: row.projectOid,
    requestType: row.requestType,
    title: row.title,
    description: row.description
  };
  showForm.value = true;
}

async function save() {
  try {
    if (editing.value) {
      await documentRequestsApi.update(editing.value.id, form.value.title, form.value.description);
    } else {
      await documentRequestsApi.create(
        form.value.projectOid, form.value.requestType,
        form.value.title, form.value.description
      );
    }
    showForm.value = false;
    msg.success('Сохранено');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function remove(row: DocumentRequest) {
  try {
    await documentRequestsApi.remove(row.id);
    msg.success('Удалено');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function submit(row: DocumentRequest) {
  try {
    await documentRequestsApi.submit(row.id);
    msg.success('Отправлено на согласование');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function resubmit(row: DocumentRequest) {
  try {
    await documentRequestsApi.resubmit(row.id);
    msg.success('Повторно отправлено');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

function statusTag(status: string | null) {
  if (!status) return h(NTag, {}, () => 'Черновик');
  const type = status === 'approved' ? 'success'
    : status === 'rejected' ? 'error'
    : status === 'pending' ? 'warning' : 'default';
  const label = status === 'approved' ? 'Согласовано'
    : status === 'rejected' ? 'Отклонено'
    : status === 'pending' ? 'На согласовании' : status;
  return h(NTag, { type }, () => label);
}

function canSubmit(row: DocumentRequest) {
  return row.currentStatus === null || row.currentStatus === 'rejected';
}

function pendingLabel(row: DocumentRequest) {
  if (!row.pendingApproverFullName && !row.pendingApproverRole) return '—';
  const role = row.pendingApproverRole ? approverRoleLabel(row.pendingApproverRole) : '';
  const step = row.pendingStepNo ? `, шаг ${row.pendingStepNo}` : '';
  if (row.currentStatus === 'rejected') {
    return `${row.pendingApproverFullName ?? '—'} (${role}) — отклонил${step}`;
  }
  if (row.currentStatus === 'pending') {
    return `${row.pendingApproverFullName ?? '—'} (${role})${step}`;
  }
  return '—';
}

const TABLE_SCROLL_X = 1430;

const columns: DataTableColumns<DocumentRequest> = [
  {
    title: 'Тип', key: 'requestType', width: 150,
    ellipsis: { tooltip: true },
    render: (r) => requestTypeLabel(r.requestType)
  },
  {
    title: 'Тема', key: 'title', width: 280,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Проект', key: 'projectName', width: 240,
    ellipsis: { tooltip: true },
    render: (r) => r.projectName ?? r.projectOid
  },
  { title: 'Статус', key: 'currentStatus', width: 140, render: (r) => statusTag(r.currentStatus) },
  {
    title: 'Согласующий', key: 'pendingApprover', width: 280,
    ellipsis: { tooltip: true },
    render: (r) => {
      const label = pendingLabel(r);
      if (label === '—') return label;
      return h(NEllipsis, { style: { maxWidth: '260px' } }, () => label);
    }
  },
  {
    title: 'Действия', key: 'actions', width: 340,
    render: (row) => h('div', { class: 't-table-actions' }, [
      h(NSpace, { size: 'small', wrap: false }, {
        default: () => [
          h(NButton, { size: 'small', onClick: () => openEdit(row) }, () => 'Изменить'),
          canSubmit(row)
            ? h(NPopconfirm, {
                onPositiveClick: () => row.currentStatus === 'rejected' ? resubmit(row) : submit(row)
              }, {
                default: () => 'Отправить на согласование?',
                trigger: () => h(NButton, { size: 'small', type: 'primary' }, () =>
                  row.currentStatus === 'rejected' ? 'Повторно' : 'На согласование')
              })
            : null,
          !row.currentStatus
            ? h(NPopconfirm, { onPositiveClick: () => remove(row) }, {
                default: () => 'Удалить заявку?',
                trigger: () => h(NButton, { size: 'small', type: 'error' }, () => 'Удалить')
              })
            : null
        ]
      })
    ])
  }
];

onMounted(async () => {
  await Promise.all([load(), loadProjects()]);
});
</script>

<template>
  <NCard title="Заявки">
    <NSpace vertical>
      <NSpace>
        <NSelect v-model:value="filterType" :options="typeOptions" placeholder="Тип" clearable style="width:220px" />
        <NInput v-model:value="search" placeholder="Поиск" clearable style="width:260px" @keyup.enter="load" />
        <NButton @click="load">Найти</NButton>
        <NButton type="primary" :disabled="!projects.length" @click="openCreate">+ Новая заявка</NButton>
      </NSpace>
      <div class="t-table-wrap">
        <NDataTable
          class="t-data-table"
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(r) => r.id"
          :scroll-x="TABLE_SCROLL_X"
          size="small"
        />
      </div>
    </NSpace>

    <AppDrawer v-model:show="showForm" :title="editing ? 'Изменить заявку' : 'Новая заявка'" width="medium">
      <NForm @submit.prevent="save">
        <NFormItem v-if="!editing" label="Проект">
          <NSelect v-model:value="form.projectOid" :options="projectOptions" />
        </NFormItem>
        <NFormItem v-if="!editing" label="Тип заявки">
          <NSelect v-model:value="form.requestType" :options="typeOptions" />
        </NFormItem>
        <NFormItem label="Тема">
          <NInput v-model:value="form.title" />
        </NFormItem>
        <NFormItem label="Описание">
          <NInput v-model:value="form.description" type="textarea" :rows="4" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="save">Сохранить</NButton>
        </NSpace>
      </NForm>
    </AppDrawer>
  </NCard>
</template>
