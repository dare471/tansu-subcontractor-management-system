<script setup lang="ts">
import { ref, computed, onMounted, h } from 'vue';
import {
  NCard, NDataTable, NButton, NTag, NForm, NFormItem, NInput, NSelect, NSpace, NCheckbox,
  useMessage
} from 'naive-ui';
import AppDrawer from '@/components/AppDrawer.vue';
import { incidentsApi, type SiteIncident } from '@/api/incidents';
import { projectsApi } from '@/api/projects';
import { subcontractorsApi } from '@/api/subcontractors';
import { toApiError } from '@/api/client';

const msg = useMessage();
const items = ref<SiteIncident[]>([]);
const loading = ref(false);
const selected = ref<SiteIncident | null>(null);
const showCreate = ref(false);
const creating = ref(false);

const projectOptions = ref<{ label: string; value: string }[]>([]);
const subcontractorOptions = ref<{ label: string; value: string }[]>([]);

const severityOptions = [
  { label: 'Низкая', value: 'low' },
  { label: 'Средняя', value: 'medium' },
  { label: 'Высокая', value: 'high' },
  { label: 'Критическая', value: 'critical' }
];

const createForm = ref({
  projectOid: null as string | null,
  subcontractorId: null as string | null,
  title: '',
  description: '',
  severity: 'medium',
  blockUntilResolved: false
});

const drawerOpen = computed({
  get: () => selected.value !== null,
  set: (open: boolean) => {
    if (!open) selected.value = null;
  }
});

function openIncident(row: SiteIncident) {
  selected.value = row;
}

const columns = [
  { title: 'Дата', key: 'occurredAt', width: 150, render: (r: SiteIncident) => new Date(r.occurredAt).toLocaleString('ru-RU') },
  { title: 'Название', key: 'title' },
  {
    title: 'Серьёзность', key: 'severity', width: 110,
    render: (r: SiteIncident) => h(NTag, { size: 'small' }, () => r.severity)
  },
  {
    title: 'Статус',
    key: 'status',
    width: 120,
    render: (r: SiteIncident) => h(NTag, { size: 'small', type: r.status === 'resolved' ? 'success' : 'warning' }, () => r.status)
  }
];

async function loadFilters() {
  const [projects, subs] = await Promise.all([projectsApi.list(), subcontractorsApi.list()]);
  projectOptions.value = projects.map((p) => ({ label: p.name || p.projectOid, value: p.projectOid }));
  subcontractorOptions.value = subs.map((s) => ({ label: s.name, value: s.id }));
}

async function load() {
  loading.value = true;
  try {
    items.value = await incidentsApi.list();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  createForm.value = {
    projectOid: projectOptions.value[0]?.value ?? null,
    subcontractorId: null,
    title: '',
    description: '',
    severity: 'medium',
    blockUntilResolved: false
  };
  showCreate.value = true;
}

async function submitCreate() {
  if (!createForm.value.projectOid) {
    msg.warning('Выберите объект.');
    return;
  }
  if (createForm.value.title.trim().length < 3) {
    msg.warning('Укажите название инцидента (мин. 3 символа).');
    return;
  }
  if (createForm.value.description.trim().length < 5) {
    msg.warning('Укажите описание (мин. 5 символов).');
    return;
  }
  creating.value = true;
  try {
    await incidentsApi.create({
      projectOid: createForm.value.projectOid,
      occurredAt: new Date().toISOString(),
      title: createForm.value.title.trim(),
      description: createForm.value.description.trim(),
      severity: createForm.value.severity,
      subcontractorId: createForm.value.subcontractorId ?? undefined,
      blockUntilResolved: createForm.value.blockUntilResolved,
      employeeIds: []
    });
    msg.success('Инцидент зарегистрирован');
    showCreate.value = false;
    await load();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    creating.value = false;
  }
}

async function resolve(id: string) {
  try {
    await incidentsApi.updateStatus(id, 'resolved', 'Расследование завершено');
    msg.success('Инцидент закрыт');
    await load();
    selected.value = null;
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

onMounted(async () => {
  await loadFilters();
  await load();
});
</script>

<template>
  <NCard title="Инциденты на объекте">
    <NSpace vertical>
      <NSpace>
        <NButton type="primary" @click="openCreate">Зарегистрировать инцидент</NButton>
        <NButton @click="load">Обновить</NButton>
      </NSpace>
      <NDataTable
        :columns="columns"
        :data="items"
        :loading="loading"
        :row-props="(row: SiteIncident) => ({ style: 'cursor:pointer', onClick: () => openIncident(row) })"
      />
    </NSpace>
  </NCard>

  <AppDrawer v-model:show="drawerOpen" title="Инцидент">
    <div v-if="selected" style="padding: 8px 0">
      <h3>{{ selected.title }}</h3>
      <p>{{ selected.description }}</p>
      <p><b>Объект:</b> {{ selected.projectName ?? selected.projectOid }}</p>
      <p v-if="selected.subcontractorName"><b>Субподрядчик:</b> {{ selected.subcontractorName }}</p>
      <p v-if="selected.blockUntilResolved"><NTag type="warning">С блокировкой до расследования</NTag></p>
      <NButton v-if="selected.status !== 'resolved'" type="primary" @click="resolve(selected.id)">Закрыть инцидент</NButton>
    </div>
  </AppDrawer>

  <AppDrawer v-model:show="showCreate" title="Новый инцидент" width="medium">
    <NForm @submit.prevent="submitCreate">
      <NFormItem label="Объект" required>
        <NSelect v-model:value="createForm.projectOid" :options="projectOptions" filterable placeholder="Проект" />
      </NFormItem>
      <NFormItem label="Субподрядчик">
        <NSelect
          v-model:value="createForm.subcontractorId"
          :options="subcontractorOptions"
          filterable
          clearable
          placeholder="Необязательно"
        />
      </NFormItem>
      <NFormItem label="Название" required>
        <NInput v-model:value="createForm.title" placeholder="Краткое описание происшествия" />
      </NFormItem>
      <NFormItem label="Описание" required>
        <NInput v-model:value="createForm.description" type="textarea" :rows="4" placeholder="Подробности" />
      </NFormItem>
      <NFormItem label="Серьёзность">
        <NSelect v-model:value="createForm.severity" :options="severityOptions" />
      </NFormItem>
      <NFormItem>
        <NCheckbox v-model:checked="createForm.blockUntilResolved">
          Заблокировать связанных сотрудников до закрытия
        </NCheckbox>
      </NFormItem>
      <NSpace justify="end">
        <NButton @click="showCreate = false">Отмена</NButton>
        <NButton type="primary" :loading="creating" @click="submitCreate">Создать</NButton>
      </NSpace>
    </NForm>
  </AppDrawer>
</template>
