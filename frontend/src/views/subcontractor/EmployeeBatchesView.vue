<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NModal, NForm, NFormItem,
  NSelect, NPopconfirm, NTag, NAlert, NProgress, NEmpty, NEllipsis, useMessage,
  type DataTableColumns
} from 'naive-ui';
import { employeeBatchesApi, type ApprovalBatch } from '@/api/employeeBatches';
import { employeesApi, type Employee } from '@/api/employees';
import { authApi, type MyProject } from '@/api/auth';
import { toApiError } from '@/api/client';
import { formatEmployees } from '@/utils/format';

const msg = useMessage();

const batches = ref<ApprovalBatch[]>([]);
const loading = ref(false);
const selected = ref<ApprovalBatch | null>(null);

const showCreate = ref(false);
const createForm = ref({ projectOid: '', title: '' });
const projects = ref<MyProject[]>([]);

const showAddMembers = ref(false);
const addCandidates = ref<Employee[]>([]);
const addSelected = ref<string[]>([]);
const addLoading = ref(false);

function batchApprovalCounts(row: ApprovalBatch) {
  const approved = row.employees.filter((e) => e.currentStatus === 'approved').length;
  const notApproved = row.employees.length - approved;
  return { approved, notApproved };
}

function renderBatchApprovalCounts(row: ApprovalBatch) {
  const { approved, notApproved } = batchApprovalCounts(row);
  return h(NSpace, { size: 4, wrap: false, justify: 'center' }, () => [
    h(NTag, { type: 'success', size: 'small', round: true }, () => `${approved} согл.`),
    h(NTag, { type: 'warning', size: 'small', round: true }, () => `${notApproved} ост.`)
  ]);
}

const BATCH_LIST_SCROLL_X = 1130;
const MEMBER_TABLE_SCROLL_X = 680;

async function load() {
  loading.value = true;
  try {
    batches.value = await employeeBatchesApi.list();
    if (selected.value) {
      selected.value = batches.value.find((b) => b.id === selected.value!.id) ?? null;
    }
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

async function loadProjects() {
  try {
    projects.value = await authApi.myProjects();
  } catch {
    projects.value = [];
  }
}

function selectBatch(row: ApprovalBatch) {
  selected.value = row;
}

function clearSelection() {
  selected.value = null;
}

function openCreate() {
  createForm.value = {
    projectOid: projects.value.find((p) => p.hasApprovalMatrix)?.projectOid ?? '',
    title: ''
  };
  showCreate.value = true;
}

async function createBatch() {
  if (!createForm.value.title.trim()) {
    msg.warning('Укажите название пакета');
    return;
  }
  try {
    const batch = await employeeBatchesApi.create(
      createForm.value.projectOid,
      createForm.value.title.trim()
    );
    msg.success('Пакет создан');
    showCreate.value = false;
    await load();
    selected.value = batch;
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

async function openAddMembers() {
  if (!selected.value || selected.value.status !== 'draft') return;
  addLoading.value = true;
  showAddMembers.value = true;
  addSelected.value = [];
  try {
    const all = await employeesApi.list({ projectOid: selected.value.projectOid });
    const inBatch = new Set(selected.value.employees.map((e) => e.employeeId));
    addCandidates.value = all.filter((e) =>
      !inBatch.has(e.id) &&
      !e.draftBatchId &&
      (e.currentStatus === null || e.currentStatus === 'rejected')
    );
  } catch (e) {
    msg.error(toApiError(e).detail);
    showAddMembers.value = false;
  } finally {
    addLoading.value = false;
  }
}

async function confirmAddMembers() {
  if (!selected.value || addSelected.value.length === 0) return;
  try {
    selected.value = await employeeBatchesApi.addMembers(selected.value.id, addSelected.value);
    msg.success('Сотрудники добавлены');
    showAddMembers.value = false;
    await load();
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

async function removeMember(employeeId: string) {
  if (!selected.value) return;
  try {
    selected.value = await employeeBatchesApi.removeMember(selected.value.id, employeeId);
    msg.success('Сотрудник исключён из пакета');
    await load();
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

async function submitBatch() {
  if (!selected.value) return;
  try {
    await employeeBatchesApi.submit(selected.value.id);
    msg.success('Пакет отправлен на согласование');
    await load();
  } catch (e) {
    const err = toApiError(e);
    if (err.code === 'batch_not_draft') msg.warning('Пакет уже отправлен');
    else msg.error(err.detail);
  }
}

async function deleteBatch() {
  if (!selected.value) return;
  try {
    await employeeBatchesApi.remove(selected.value.id);
    msg.success('Пакет удалён');
    selected.value = null;
    await load();
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

async function submitBatchById(batch: ApprovalBatch) {
  selected.value = batch;
  await submitBatch();
}

async function deleteBatchById(batch: ApprovalBatch) {
  selected.value = batch;
  await deleteBatch();
}

function stopRowClick(e: Event) {
  e.stopPropagation();
}

function statusTag(status: string) {
  if (status === 'draft') return h(NTag, { type: 'default', round: true }, () => 'Черновик');
  if (status === 'submitted') return h(NTag, { type: 'warning', round: true }, () => 'На согласовании');
  return h(NTag, { round: true }, () => status);
}

function memberStatusTag(status: string | null) {
  if (!status) return h(NTag, { size: 'small', round: true }, () => 'Черновик');
  const map: Record<string, { type: 'success' | 'error' | 'warning' | 'default'; label: string }> = {
    approved: { type: 'success', label: 'Согласован' },
    rejected: { type: 'error', label: 'Отклонён' },
    pending: { type: 'warning', label: 'На согласовании' }
  };
  const m = map[status] ?? { type: 'default' as const, label: status };
  return h(NTag, { size: 'small', type: m.type, round: true }, () => m.label);
}

function renderBatchActions(row: ApprovalBatch) {
  const buttons = [
    h(NButton, { size: 'small', onClick: () => selectBatch(row) }, () => 'Открыть')
  ];
  if (row.status === 'draft') {
    buttons.push(
      h(NPopconfirm, { onPositiveClick: () => submitBatchById(row) }, {
        default: () => 'Отправить пакет на согласование?',
        trigger: () => h(NButton, {
          size: 'small',
          type: 'warning',
          disabled: row.employeeCount === 0
        }, () => 'Отправить')
      }),
      h(NPopconfirm, { onPositiveClick: () => deleteBatchById(row) }, {
        default: () => 'Удалить черновик?',
        trigger: () => h(NButton, { size: 'small', type: 'error' }, () => 'Удалить')
      })
    );
  }
  return h('div', { class: 't-table-actions', onClick: stopRowClick }, [
    h(NSpace, { size: 'small', wrap: false }, { default: () => buttons })
  ]);
}

const batchColumns: DataTableColumns<ApprovalBatch> = [
  {
    title: 'Название', key: 'title', width: 220,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Проект', key: 'projectName', width: 200,
    ellipsis: { tooltip: true },
    render: (r) => r.projectName ?? r.projectOid
  },
  {
    title: 'Согласование', key: 'approval', width: 200, align: 'center',
    render: (r) => renderBatchApprovalCounts(r)
  },
  { title: 'Статус', key: 'status', width: 140, render: (r) => statusTag(r.status) },
  {
    title: 'Создан', key: 'createdAt', width: 110,
    render: (r) => new Date(r.createdAt).toLocaleDateString('ru-RU')
  },
  {
    title: 'Действия', key: 'actions', width: 260,
    render: (r) => renderBatchActions(r)
  }
];

const memberColumns = computed<DataTableColumns<ApprovalBatch['employees'][0]>>(() => {
  const draft = selected.value?.status === 'draft';
  return [
    {
      title: 'ФИО', key: 'fullName', width: 200,
      ellipsis: { tooltip: true }
    },
    {
      title: 'Должность', key: 'position', width: 180,
      ellipsis: { tooltip: true }
    },
    { title: 'Статус', key: 'currentStatus', width: 140, render: (r) => memberStatusTag(r.currentStatus) },
    ...(draft ? [{
      title: 'Действия',
      key: 'actions',
      width: 120,
      render: (r: ApprovalBatch['employees'][0]) => h('div', {
        class: 't-table-actions',
        onClick: stopRowClick
      }, [
        h(NPopconfirm, {
          onPositiveClick: () => removeMember(r.employeeId)
        }, {
          default: () => 'Исключить из пакета?',
          trigger: () => h(NButton, { size: 'small', quaternary: true, type: 'error' }, () => 'Убрать')
        })
      ])
    }] : [])
  ];
});

const addColumns: DataTableColumns<Employee> = [
  { type: 'selection', width: 48 },
  { title: 'ФИО', key: 'fullName', width: 200, ellipsis: { tooltip: true } },
  { title: 'Должность', key: 'position', width: 180, ellipsis: { tooltip: true } },
  { title: 'ИИН', key: 'iin', width: 130 }
];

const ADD_TABLE_SCROLL_X = 560;

const stats = computed(() => {
  const b = selected.value;
  if (!b) return null;
  const approved = b.employees.filter((e) => e.currentStatus === 'approved').length;
  const pending = b.employees.filter((e) => e.currentStatus === 'pending').length;
  const rejected = b.employees.filter((e) => e.currentStatus === 'rejected').length;
  const draft = b.employees.filter((e) => !e.currentStatus).length;
  const progress = b.status === 'submitted' && b.employeeCount > 0
    ? Math.round((approved / b.employeeCount) * 100)
    : 0;
  return { approved, pending, rejected, draft, progress, total: b.employeeCount };
});

const projectOptions = computed(() =>
  projects.value
    .filter((p) => p.hasApprovalMatrix)
    .map((p) => ({ label: p.name || p.projectOid, value: p.projectOid }))
);

const formattedSubmittedAt = computed(() => {
  if (!selected.value?.submittedAt) return null;
  return new Date(selected.value.submittedAt).toLocaleString('ru-RU', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
});

onMounted(async () => {
  await Promise.all([loadProjects(), load()]);
});
</script>

<template>
  <div>
    <div class="t-batch-layout">
    <NCard title="Пакеты согласования" :bordered="true">
      <NSpace vertical :size="12">
        <NSpace wrap>
          <NButton type="primary" :disabled="!projectOptions.length" @click="openCreate">
            + Новый пакет
          </NButton>
          <NButton :loading="loading" @click="load">Обновить</NButton>
        </NSpace>

        <div class="t-table-wrap">
          <NDataTable
            class="t-data-table"
            :columns="batchColumns"
            :data="batches"
            :loading="loading"
            :row-key="(r) => r.id"
            :scroll-x="BATCH_LIST_SCROLL_X"
            size="small"
            :row-props="(row) => ({
              style: selected?.id === row.id
                ? 'cursor:pointer;background:var(--brand-orange-soft)'
                : 'cursor:pointer',
              onClick: () => selectBatch(row)
            })"
          />
        </div>

        <NEmpty v-if="!loading && !batches.length" description="Пакетов пока нет" />
      </NSpace>
    </NCard>

    <NCard v-if="selected" :bordered="true">
      <template #header>
        <div class="t-batch-detail__header" style="border:none;padding:0;width:100%">
          <div style="flex:1;min-width:0">
            <h2 class="t-batch-detail__title">
              <NEllipsis style="max-width:100%">{{ selected.title }}</NEllipsis>
            </h2>
            <div class="t-batch-detail__meta">
              <NTag :type="selected.status === 'draft' ? 'default' : 'warning'" round size="small">
                {{ selected.status === 'draft' ? 'Черновик' : 'На согласовании' }}
              </NTag>
              <span>{{ formatEmployees(selected.employeeCount) }}</span>
              <span>{{ selected.projectName ?? selected.projectOid }}</span>
              <span v-if="formattedSubmittedAt">Отправлен {{ formattedSubmittedAt }}</span>
            </div>
          </div>
          <NButton quaternary size="small" @click="clearSelection">Закрыть</NButton>
        </div>
      </template>

      <NSpace vertical :size="16">
        <div v-if="stats && selected.status === 'submitted'" class="t-batch-detail__stats">
          <div class="t-batch-stat t-batch-stat--success">
            <div class="t-batch-stat__label">Согласовано</div>
            <div class="t-batch-stat__value">{{ stats.approved }}</div>
          </div>
          <div class="t-batch-stat t-batch-stat--warning">
            <div class="t-batch-stat__label">На согласовании</div>
            <div class="t-batch-stat__value">{{ stats.pending }}</div>
          </div>
          <div class="t-batch-stat t-batch-stat--error">
            <div class="t-batch-stat__label">Отклонено</div>
            <div class="t-batch-stat__value">{{ stats.rejected }}</div>
          </div>
          <div class="t-batch-stat">
            <div class="t-batch-stat__label">Прогресс</div>
            <NProgress
              type="line"
              :percentage="stats.progress"
              :height="8"
              :border-radius="4"
              style="margin-top:8px"
            />
          </div>
        </div>

        <div v-if="selected.status === 'draft'" class="t-batch-detail__toolbar">
          <NButton type="primary" @click="openAddMembers">Добавить сотрудников</NButton>
          <NPopconfirm @positive-click="submitBatch">
            <template #default>Отправить пакет на согласование?</template>
            <template #trigger>
              <NButton type="warning" :disabled="selected.employeeCount === 0">Отправить</NButton>
            </template>
          </NPopconfirm>
          <NPopconfirm @positive-click="deleteBatch">
            <template #default>Удалить черновик?</template>
            <template #trigger>
              <NButton quaternary type="error">Удалить</NButton>
            </template>
          </NPopconfirm>
        </div>

        <div>
          <h3 class="t-section-title">Состав пакета</h3>
          <NEmpty
            v-if="!selected.employees.length"
            description="В пакете пока нет сотрудников"
            style="padding:24px 0"
          />
          <div v-else class="t-table-wrap">
            <NDataTable
              class="t-data-table"
              :columns="memberColumns"
              :data="selected.employees"
              :row-key="(r) => r.employeeId"
              :scroll-x="MEMBER_TABLE_SCROLL_X"
              size="small"
            />
          </div>
        </div>
      </NSpace>
    </NCard>

    <NCard v-else title="Детали пакета" :bordered="true">
      <NEmpty description="Выберите пакет в списке выше" style="padding:32px 0" />
    </NCard>
    </div>

    <NModal v-model:show="showCreate" preset="card" title="Новый пакет" style="width:480px">
      <NForm @submit.prevent="createBatch">
        <NFormItem label="Проект">
          <NSelect v-model:value="createForm.projectOid" :options="projectOptions" />
        </NFormItem>
        <NFormItem label="Название">
          <NInput v-model:value="createForm.title" placeholder="Бригада монтажа — май" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showCreate = false">Отмена</NButton>
          <NButton type="primary" @click="createBatch">Создать</NButton>
        </NSpace>
      </NForm>
    </NModal>

    <NModal
      v-model:show="showAddMembers"
      preset="card"
      title="Добавить сотрудников"
      style="width:min(720px,96vw)"
    >
      <NAlert v-if="!addLoading && !addCandidates.length" type="warning" :bordered="false" style="margin-bottom:12px">
        Нет доступных сотрудников для этого проекта.
      </NAlert>
      <div class="t-table-wrap">
        <NDataTable
          v-model:checked-row-keys="addSelected"
          class="t-data-table"
          :columns="addColumns"
          :data="addCandidates"
          :loading="addLoading"
          :row-key="(r) => r.id"
          :scroll-x="ADD_TABLE_SCROLL_X"
          size="small"
        />
      </div>
      <NSpace justify="end" style="margin-top:16px">
        <NButton @click="showAddMembers = false">Отмена</NButton>
        <NButton type="primary" :disabled="!addSelected.length" @click="confirmAddMembers">
          Добавить ({{ addSelected.length }})
        </NButton>
      </NSpace>
    </NModal>
  </div>
</template>
