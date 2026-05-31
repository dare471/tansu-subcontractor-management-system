<script setup lang="ts">
import { ref, onMounted, h } from 'vue';
import { useRouter } from 'vue-router';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NSelect, NTag,
  useMessage, type DataTableColumns
} from 'naive-ui';
import { employeesApi, type Employee } from '@/api/employees';
import { subcontractorsApi } from '@/api/subcontractors';
import { toApiError } from '@/api/client';

const msg = useMessage();
const router = useRouter();
const items = ref<Employee[]>([]);
const loading = ref(false);
const search = ref('');
const subcontractorId = ref<string | null>(null);
const subOptions = ref<{ label: string; value: string }[]>([]);

async function load() {
  loading.value = true;
  try {
    items.value = await employeesApi.list({
      search: search.value || undefined,
      subcontractorId: subcontractorId.value ?? undefined
    });
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

async function loadSubs() {
  const subs = await subcontractorsApi.list();
  subOptions.value = subs.map((s) => ({ label: s.name, value: s.id }));
}

const columns: DataTableColumns<Employee> = [
  { title: 'ФИО', key: 'fullName', ellipsis: { tooltip: true } },
  { title: 'Должность', key: 'position', ellipsis: { tooltip: true } },
  { title: 'Субподрядчик', key: 'subcontractorName', ellipsis: { tooltip: true } },
  { title: 'Объект', key: 'projectName', ellipsis: { tooltip: true } },
  {
    title: 'Статус', key: 'status', width: 140,
    render: (r) => {
      if (r.isBlocked) return h(NTag, { type: 'error' }, () => 'Заблокирован');
      if (!r.currentStatus) return h(NTag, {}, () => 'Черновик');
      const type = r.currentStatus === 'approved' ? 'success'
        : r.currentStatus === 'rejected' ? 'error' : 'warning';
      const label = r.currentStatus === 'approved' ? 'Согласован'
        : r.currentStatus === 'rejected' ? 'Отклонён' : 'На согласовании';
      return h(NTag, { type }, () => label);
    }
  },
  {
    title: '', key: 'a', width: 120,
    render: (r) => h(NButton, {
      size: 'small',
      onClick: () => router.push({ name: 'employee-approvals', params: { id: r.id } })
    }, () => 'Карточка')
  }
];

onMounted(async () => {
  await loadSubs();
  await load();
});
</script>

<template>
  <NCard title="Сотрудники субподрядчиков">
    <NSpace vertical>
      <NSpace wrap>
        <NInput v-model:value="search" placeholder="Поиск по ФИО или ИИН" clearable style="width:260px" @keyup.enter="load" />
        <NSelect
          v-model:value="subcontractorId"
          :options="subOptions"
          placeholder="Субподрядчик"
          clearable
          filterable
          style="width:240px"
        />
        <NButton type="primary" @click="load">Найти</NButton>
      </NSpace>
      <NDataTable :columns="columns" :data="items" :loading="loading" :row-key="(r) => r.id" size="small" />
    </NSpace>
  </NCard>
</template>
