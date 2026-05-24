<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue';
import { useRouter } from 'vue-router';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NModal, NForm, NFormItem,
  NPopconfirm, NSelect, NEmpty, useMessage, type DataTableColumns, type SelectOption
} from 'naive-ui';
import { subcontractorsApi, type Subcontractor, type ProjectBinding } from '@/api/subcontractors';
import { projectsApi, type Project } from '@/api/projects';
import { toApiError } from '@/api/client';

const msg = useMessage();
const router = useRouter();
const items = ref<Subcontractor[]>([]);
const loading = ref(false);
const search = ref('');

const showForm = ref(false);
const editing = ref<Subcontractor | null>(null);
const form = ref({ name: '', bin: '' });

const showBindings = ref(false);
const bindTarget = ref<Subcontractor | null>(null);
const bindings = ref<ProjectBinding[]>([]);
const projects = ref<Project[]>([]);
const selectedProjectOid = ref<string | null>(null);
const bindingsLoading = ref(false);
const bindingSaving = ref(false);

async function load() {
  loading.value = true;
  try {
    items.value = await subcontractorsApi.list(search.value || undefined);
  } catch (e) { msg.error(toApiError(e).detail); } finally { loading.value = false; }
}

function openCreate() {
  editing.value = null;
  form.value = { name: '', bin: '' };
  showForm.value = true;
}

function openEdit(row: Subcontractor) {
  editing.value = row;
  form.value = { name: row.name, bin: row.bin };
  showForm.value = true;
}

async function save() {
  try {
    if (editing.value)
      await subcontractorsApi.update(editing.value.id, form.value.name, form.value.bin);
    else
      await subcontractorsApi.create(form.value.name, form.value.bin);
    showForm.value = false;
    await load();
    msg.success('Сохранено');
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function remove(row: Subcontractor) {
  try {
    await subcontractorsApi.remove(row.id);
    msg.success('Удалено');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function openBindings(row: Subcontractor) {
  bindTarget.value = row;
  showBindings.value = true;
  selectedProjectOid.value = null;
  bindingsLoading.value = true;
  try {
    const [bound, all] = await Promise.all([
      subcontractorsApi.projects(row.id),
      projectsApi.list()
    ]);
    bindings.value = bound;
    projects.value = all;
  } catch (e) {
    msg.error(toApiError(e).detail);
    bindings.value = [];
    projects.value = [];
  } finally {
    bindingsLoading.value = false;
  }
}

const availableProjectOptions = computed<SelectOption[]>(() => {
  const bound = new Set(bindings.value.map((b) => b.projectOid));
  return projects.value
    .filter((p) => !bound.has(p.projectOid))
    .map((p) => ({
      label: p.name ? `${p.name} (${p.projectOid})` : p.projectOid,
      value: p.projectOid
    }));
});

async function bindProject() {
  if (!bindTarget.value || !selectedProjectOid.value) return;
  bindingSaving.value = true;
  try {
    const project = projects.value.find((p) => p.projectOid === selectedProjectOid.value);
    await subcontractorsApi.bindProject(
      bindTarget.value.id,
      selectedProjectOid.value,
      project?.name ?? undefined
    );
    bindings.value = await subcontractorsApi.projects(bindTarget.value.id);
    selectedProjectOid.value = null;
    await load();
    msg.success('Проект привязан');
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { bindingSaving.value = false; }
}

async function unbindProject(projectOid: string) {
  if (!bindTarget.value) return;
  try {
    await subcontractorsApi.unbindProject(bindTarget.value.id, projectOid);
    bindings.value = await subcontractorsApi.projects(bindTarget.value.id);
    await load();
    msg.success('Привязка удалена');
  } catch (e) { msg.error(toApiError(e).detail); }
}

const columns: DataTableColumns<Subcontractor> = [
  { title: 'Наименование', key: 'name' },
  { title: 'БИН', key: 'bin', width: 160 },
  { title: 'Проектов', key: 'projectsCount', width: 100 },
  { title: 'Пользователей', key: 'usersCount', width: 130 },
  {
    title: 'Действия', key: 'actions', width: 380,
    render: (row) => h(NSpace, { size: 'small' }, () => [
      h(NButton, { size: 'small', onClick: () => openEdit(row) }, () => 'Изменить'),
      h(NButton, { size: 'small', onClick: () => openBindings(row) }, () => 'Проекты'),
      h(NPopconfirm, {
        onPositiveClick: () => remove(row)
      }, {
        default: () => 'Удалить субподрядчика?',
        trigger: () => h(NButton, { size: 'small', type: 'error' }, () => 'Удалить')
      })
    ])
  }
];

const bindingColumns = [
  { title: 'OID', key: 'projectOid', width: 320 },
  { title: 'Название', key: 'name', render: (row: ProjectBinding) => row.name || '—' },
  {
    title: '', key: 'a', width: 120,
    render: (row: ProjectBinding) =>
      h(NButton, { size: 'small', type: 'error', onClick: () => unbindProject(row.projectOid) }, () => 'Отвязать')
  }
];

onMounted(load);
</script>

<template>
  <NCard title="Субподрядчики">
    <NSpace vertical>
      <NSpace>
        <NInput v-model:value="search" placeholder="Поиск по названию или БИН" clearable @keyup.enter="load" style="width:300px" />
        <NButton @click="load">Найти</NButton>
        <NButton type="primary" @click="openCreate">+ Новый</NButton>
      </NSpace>
      <NDataTable :columns="columns" :data="items" :loading="loading" :row-key="(row) => row.id" />
    </NSpace>

    <NModal v-model:show="showForm" preset="card" :title="editing ? 'Изменить субподрядчика' : 'Новый субподрядчик'" style="width:480px">
      <NForm @submit.prevent="save">
        <NFormItem label="Наименование">
          <NInput v-model:value="form.name" />
        </NFormItem>
        <NFormItem label="БИН">
          <NInput v-model:value="form.bin" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="save">Сохранить</NButton>
        </NSpace>
      </NForm>
    </NModal>

    <NModal
      v-model:show="showBindings"
      preset="card"
      :title="'Проекты субподрядчика «' + (bindTarget?.name ?? '') + '»'"
      style="width:720px"
    >
      <NSpace vertical :size="16">
        <div v-if="!projects.length && !bindingsLoading">
          <NEmpty description="Нет зарегистрированных проектов">
            <template #extra>
              <NButton type="primary" @click="router.push({ name: 'projects' })">
                Перейти к регистрации проектов
              </NButton>
            </template>
          </NEmpty>
        </div>

        <NSpace v-else align="center" style="width:100%">
          <NSelect
            v-model:value="selectedProjectOid"
            :options="availableProjectOptions"
            placeholder="Выберите проект для привязки"
            filterable
            clearable
            style="flex:1;min-width:320px"
            :disabled="bindingsLoading || !availableProjectOptions.length"
          />
          <NButton
            type="primary"
            :disabled="!selectedProjectOid"
            :loading="bindingSaving"
            @click="bindProject"
          >
            Привязать
          </NButton>
        </NSpace>

        <p v-if="projects.length && !availableProjectOptions.length && !bindingsLoading" style="color:var(--brand-text-muted);font-size:13px;margin:0">
          Все доступные проекты уже привязаны к этому субподрядчику.
        </p>

        <NDataTable
          :columns="bindingColumns"
          :data="bindings"
          :loading="bindingsLoading"
          :row-key="(r) => r.projectOid"
        />
      </NSpace>
    </NModal>
  </NCard>
</template>
