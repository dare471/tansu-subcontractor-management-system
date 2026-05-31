<script setup lang="ts">
import { ref, onMounted } from 'vue';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NSelect, NPagination,
  useMessage, type DataTableColumns
} from 'naive-ui';
import { siteVisitJournalApi, type SiteVisitJournalItem } from '@/api/siteVisitJournal';
import { subcontractorsApi } from '@/api/subcontractors';
import { projectsApi } from '@/api/projects';
import { toApiError } from '@/api/client';

const msg = useMessage();
const items = ref<SiteVisitJournalItem[]>([]);
const loading = ref(false);
const exporting = ref<'excel' | 'pdf' | null>(null);
const totalCount = ref(0);
const page = ref(1);
const pageSize = ref(50);

const search = ref('');
const subcontractorId = ref<string | null>(null);
const projectOid = ref<string | null>(null);
const fromDate = ref('');
const toDate = ref('');

const subcontractorOptions = ref<{ label: string; value: string }[]>([]);
const projectOptions = ref<{ label: string; value: string }[]>([]);

function currentFilters() {
  return {
    search: search.value.trim() || undefined,
    subcontractorId: subcontractorId.value ?? undefined,
    projectOid: projectOid.value ?? undefined,
    from: fromDate.value ? new Date(fromDate.value).toISOString() : undefined,
    to: toDate.value ? new Date(toDate.value).toISOString() : undefined
  };
}

async function loadFilters() {
  try {
    const [subs, projects] = await Promise.all([
      subcontractorsApi.list(),
      projectsApi.list()
    ]);
    subcontractorOptions.value = subs.map((s) => ({ label: s.name, value: s.id }));
    projectOptions.value = projects.map((p) => ({
      label: p.name || p.projectOid,
      value: p.projectOid
    }));
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

async function load() {
  loading.value = true;
  try {
    const result = await siteVisitJournalApi.list({
      page: page.value,
      pageSize: pageSize.value,
      ...currentFilters()
    });
    items.value = result.items;
    totalCount.value = result.totalCount;
    page.value = result.page;
    pageSize.value = result.pageSize;
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

async function exportJournal(format: 'excel' | 'pdf') {
  exporting.value = format;
  try {
    await siteVisitJournalApi.exportFile(format, currentFilters());
    msg.success(format === 'excel' ? 'Файл Excel (CSV) загружен' : 'PDF загружен');
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    exporting.value = null;
  }
}

function resetFilters() {
  search.value = '';
  subcontractorId.value = null;
  projectOid.value = null;
  fromDate.value = '';
  toDate.value = '';
  page.value = 1;
  load();
}

function formatDate(value: string | null) {
  if (!value) return '—';
  return new Date(value).toLocaleString('ru-RU');
}

const TABLE_SCROLL_X = 1520;

const columns: DataTableColumns<SiteVisitJournalItem> = [
  { title: 'ФИО', key: 'employeeFullName', width: 180, ellipsis: { tooltip: true } },
  { title: 'Субподрядчик', key: 'subcontractorName', width: 180, ellipsis: { tooltip: true } },
  {
    title: 'Объект', key: 'projectName', width: 180,
    ellipsis: { tooltip: true },
    render: (r) => r.projectName ?? '—'
  },
  {
    title: 'Терминал СКУД', key: 'terminalLocation', width: 160,
    ellipsis: { tooltip: true },
    render: (r) => r.terminalLocation ?? '—'
  },
  { title: 'Вход', key: 'checkedInAt', width: 160, render: (r) => formatDate(r.checkedInAt) },
  { title: 'Выход', key: 'checkedOutAt', width: 160, render: (r) => formatDate(r.checkedOutAt) },
  { title: 'Источник', key: 'dataSourceLabel', width: 130 },
  {
    title: 'Face ID', key: 'faceConfidence', width: 90, align: 'center',
    render: (r) => r.faceConfidence != null ? `${(r.faceConfidence * 100).toFixed(1)}%` : '—'
  }
];

onMounted(async () => {
  await loadFilters();
  await load();
});
</script>

<template>
  <NCard title="Журнал посещений">
    <NSpace vertical>
      <NSpace wrap>
        <NInput
          v-model:value="search"
          placeholder="Сотрудник: ФИО или ИИН"
          clearable
          style="width:240px"
          @keyup.enter="() => { page = 1; load(); }"
        />
        <NSelect
          v-model:value="subcontractorId"
          :options="subcontractorOptions"
          placeholder="Субпodрядчик"
          clearable
          filterable
          style="width:220px"
        />
        <NSelect
          v-model:value="projectOid"
          :options="projectOptions"
          placeholder="Проект / объект"
          clearable
          filterable
          style="width:220px"
        />
        <NInput v-model:value="fromDate" type="date" placeholder="С" style="width:150px" />
        <NInput v-model:value="toDate" type="date" placeholder="По" style="width:150px" />
        <NButton type="primary" @click="() => { page = 1; load(); }">Найти</NButton>
        <NButton @click="resetFilters">Сбросить</NButton>
        <NButton :loading="exporting === 'excel'" @click="exportJournal('excel')">Excel</NButton>
        <NButton :loading="exporting === 'pdf'" @click="exportJournal('pdf')">PDF</NButton>
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

      <NSpace justify="end">
        <NPagination
          v-model:page="page"
          v-model:page-size="pageSize"
          :item-count="totalCount"
          :page-sizes="[25, 50, 100, 200]"
          show-size-picker
          @update:page="load"
          @update:page-size="() => { page = 1; load(); }"
        />
      </NSpace>
    </NSpace>
  </NCard>
</template>
