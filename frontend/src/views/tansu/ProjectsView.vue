<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { NCard, NSpace, NInput, NButton, NDataTable, NModal, NForm, NFormItem, useMessage } from 'naive-ui';
import { projectsApi, type Project } from '@/api/projects';
import { toApiError } from '@/api/client';

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

const columns = [
  { title: 'OID проекта', key: 'projectOid', width: 320 },
  { title: 'Название', key: 'name' },
  { title: 'Привязано субподрядчиков', key: 'subcontractorsCount', width: 220 }
];

onMounted(load);
</script>

<template>
  <NCard title="Проекты">
    <NSpace vertical>
      <NSpace>
        <NInput v-model:value="search" placeholder="Поиск по названию" clearable @keyup.enter="load" style="width:300px" />
        <NButton @click="load">Найти</NButton>
        <NButton type="primary" @click="showForm = true">+ Зарегистрировать</NButton>
      </NSpace>
      <NDataTable :columns="columns" :data="items" :loading="loading" :row-key="(r) => r.projectOid" />
    </NSpace>

    <NModal v-model:show="showForm" preset="card" title="Регистрация проекта" style="width:480px">
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
    </NModal>
  </NCard>
</template>
