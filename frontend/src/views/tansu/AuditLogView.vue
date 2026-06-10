<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { NCard, NDataTable, NInput, NDatePicker, NButton, NSpace, NDrawer, NCode, useMessage } from 'naive-ui';
import { auditApi, type AuditEvent } from '@/api/audit';
import { toApiError } from '@/api/client';

const msg = useMessage();
const items = ref<AuditEvent[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(50);
const loading = ref(false);
const actionFilter = ref('');
const selected = ref<AuditEvent | null>(null);
const drawerOpen = ref(false);
const range = ref<[number, number] | null>(null);

const columns = [
  { title: 'Время', key: 'occurredAt', width: 160, render: (r: AuditEvent) => new Date(r.occurredAt).toLocaleString('ru-RU') },
  { title: 'Действие', key: 'action', width: 180 },
  { title: 'Сводка', key: 'summary', ellipsis: { tooltip: true } },
  { title: 'Кто', key: 'actorEmail', width: 200, render: (r: AuditEvent) => r.actorEmail ?? r.actorType }
];

async function load() {
  loading.value = true;
  try {
    const from = range.value ? new Date(range.value[0]).toISOString() : undefined;
    const to = range.value ? new Date(range.value[1]).toISOString() : undefined;
    const res = await auditApi.list({
      page: page.value,
      pageSize: pageSize.value,
      action: actionFilter.value || undefined,
      from,
      to
    });
    items.value = res.items;
    total.value = res.total;
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

function openRow(row: AuditEvent) {
  selected.value = row;
  drawerOpen.value = true;
}

onMounted(load);
</script>

<template>
  <NCard title="Журнал действий">
    <NSpace vertical>
      <NSpace>
        <NInput v-model:value="actionFilter" placeholder="Фильтр по action" clearable style="width: 240px" />
        <NDatePicker v-model:value="range" type="datetimerange" clearable />
        <NButton type="primary" @click="load">Обновить</NButton>
      </NSpace>
      <NDataTable
        :columns="columns"
        :data="items"
        :loading="loading"
        :pagination="{ page, pageSize, itemCount: total, onUpdatePage: (p: number) => { page = p; load(); } }"
        :row-props="(row: AuditEvent) => ({ style: 'cursor:pointer', onClick: () => openRow(row) })"
      />
    </NSpace>
  </NCard>
  <NDrawer v-model:show="drawerOpen" :width="520" placement="right">
    <div v-if="selected" style="padding: 24px">
      <h3>{{ selected.summary }}</h3>
      <p><b>Action:</b> {{ selected.action }}</p>
      <p><b>Entity:</b> {{ selected.entityType }} / {{ selected.entityId }}</p>
      <NCode v-if="selected.payloadJson" :code="selected.payloadJson" language="json" word-wrap />
    </div>
  </NDrawer>
</template>
