<script setup lang="ts">
import { ref, onMounted, h } from 'vue';
import { useRouter } from 'vue-router';
import { NCard, NSpace, NInput, NButton, NDataTable, NForm, NFormItem, useMessage } from 'naive-ui';
import AppDrawer from '@/components/AppDrawer.vue';
import { projectsApi, type Project } from '@/api/projects';
import { toApiError } from '@/api/client';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const auth = useAuthStore();
const msg = useMessage();
const items = ref<Project[]>([]);
const loading = ref(false);
const search = ref('');
const showForm = ref(false);
const form = ref({ projectOid: '', name: '' });

async function load() {
  loading.value = true;
  try { items.value = await projectsApi.list(search.value || undefined); }
  catch (e) { msg.error(toApiError(e).detail); }
  finally { loading.value = false; }
}

async function register() {
  try {
    await projectsApi.register(form.value.projectOid.trim(), form.value.name.trim());
    showForm.value = false;
    msg.success('Проект зарегистрирован');
    form.value = { projectOid: '', name: '' };
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

function openProject(row: Project) {
  router.push({ name: 'project-detail', params: { projectOid: row.projectOid } });
}

const columns = [
  {
    title: 'Название', key: 'name',
    render: (row: Project) => row.name ?? '—',
    ellipsis: { tooltip: true }
  },
  { title: 'OID проекта', key: 'projectOid', width: 320 },
  { title: 'Субподрядчиков', key: 'subcontractorsCount', width: 160 },
  {
    title: '', key: 'open', width: 120,
    render: (row: Project) =>
      h(NButton, { size: 'small', onClick: () => openProject(row) }, () => 'Открыть')
  }
];

onMounted(load);
</script>

<template>
  <NCard title="Проекты">
    <NSpace vertical>
      <NSpace>
        <NInput v-model:value="search" placeholder="Поиск по названию" clearable @keyup.enter="load" style="width:300px" />
        <NButton @click="load">Найти</NButton>
        <NButton v-if="auth.canManageProjects" type="primary" @click="showForm = true">+ Зарегистрировать</NButton>
      </NSpace>
      <div class="t-table-wrap">
        <NDataTable
          class="t-data-table"
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(r) => r.projectOid"
          size="small"
          :row-props="(row) => ({ style: 'cursor:pointer', onClick: () => openProject(row) })"
        />
      </div>
    </NSpace>

    <AppDrawer v-model:show="showForm" title="Регистрация проекта" width="narrow">
      <NForm @submit.prevent="register">
        <NFormItem label="OID проекта (uuid)">
          <NInput v-model:value="form.projectOid" placeholder="UUID проекта из ERP" />
        </NFormItem>
        <NFormItem label="Название (опционально)">
          <NInput v-model:value="form.name" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="register">Сохранить</NButton>
        </NSpace>
      </NForm>
    </AppDrawer>
  </NCard>
</template>
