<script setup lang="ts">
import { ref, onMounted, h, computed } from 'vue';
import {
  NCard, NSpace, NDataTable, NInputNumber, NButton, NProgress, NEmpty, useMessage,
  type DataTableColumns
} from 'naive-ui';
import { authApi, type MyProject } from '@/api/auth';
import { toApiError } from '@/api/client';

const msg = useMessage();
const projects = ref<MyProject[]>([]);
const loading = ref(false);
const savingOid = ref<string | null>(null);
const draftPercent = ref<Record<string, number>>({});

async function load() {
  loading.value = true;
  try {
    projects.value = await authApi.myProjects();
    for (const p of projects.value)
      draftPercent.value[p.projectOid] = p.completionPercent;
  } catch (e) {
    msg.error(toApiError(e).detail);
    projects.value = [];
  } finally {
    loading.value = false;
  }
}

async function submit(row: MyProject) {
  const value = draftPercent.value[row.projectOid];
  if (value == null || value < 0 || value > 100) {
    msg.warning('Укажите процент от 0 до 100');
    return;
  }
  savingOid.value = row.projectOid;
  try {
    await authApi.reportProjectProgress(row.projectOid, value);
    await load();
    msg.success('Отчёт отправлен');
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { savingOid.value = null; }
}

const columns = computed<DataTableColumns<MyProject>>(() => [
  {
    title: 'Проект', key: 'name', ellipsis: { tooltip: true },
    render: (r) => r.name || r.projectOid
  },
  { title: 'Вид деятельности', key: 'activityType', width: 220, ellipsis: { tooltip: true } },
  {
    title: 'Текущий %', key: 'completionPercent', width: 140,
    render: (r) => h(NProgress, {
      type: 'line',
      percentage: r.completionPercent,
      indicatorPlacement: 'inside'
    })
  },
  {
    title: 'Последний отчёт', key: 'progressReportedAt', width: 140,
    render: (r) => r.progressReportedAt
      ? new Date(r.progressReportedAt).toLocaleDateString('ru-RU')
      : '—'
  },
  {
    title: 'Новый %', key: 'draft', width: 140,
    render: (r) => h(NInputNumber, {
      value: draftPercent.value[r.projectOid],
      min: 0,
      max: 100,
      size: 'small',
      style: 'width:100%',
      onUpdateValue: (v: number | null) => {
        if (v != null) draftPercent.value[r.projectOid] = v;
      }
    })
  },
  {
    title: '', key: 'actions', width: 120,
    render: (r) => h(NButton, {
      size: 'small',
      type: 'primary',
      loading: savingOid.value === r.projectOid,
      onClick: () => submit(r)
    }, () => 'Отправить')
  }
]);

onMounted(load);
</script>

<template>
  <NCard title="Отчётность по проектам">
    <NSpace vertical>
      <p style="margin:0;color:var(--brand-text-muted);font-size:13px">
        Укажите процент выполнения работ по каждому проекту. Данные видны сотрудникам ТАНСУ в карточке проекта.
      </p>
      <NEmpty v-if="!loading && !projects.length" description="Нет привязанных проектов" />
      <div v-else class="t-table-wrap">
        <NDataTable
          :columns="columns"
          :data="projects"
          :loading="loading"
          :row-key="(r) => r.projectOid"
          :scroll-x="960"
          size="small"
        />
      </div>
    </NSpace>
  </NCard>
</template>
