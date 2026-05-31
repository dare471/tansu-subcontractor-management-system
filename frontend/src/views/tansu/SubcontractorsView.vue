<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue';
import { useRouter } from 'vue-router';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NModal, NForm, NFormItem,
  NPopconfirm, NSelect, NEmpty, NTag, useMessage, type DataTableColumns, type SelectOption
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
const bindActivityType = ref('');
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
  bindActivityType.value = '';
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
  if (!bindActivityType.value.trim()) {
    msg.warning('Укажите вид деятельности на проекте');
    return;
  }
  bindingSaving.value = true;
  try {
    const project = projects.value.find((p) => p.projectOid === selectedProjectOid.value);
    await subcontractorsApi.bindProject(
      bindTarget.value.id,
      selectedProjectOid.value,
      bindActivityType.value.trim(),
      project?.name ?? undefined
    );
    bindings.value = await subcontractorsApi.projects(bindTarget.value.id);
    selectedProjectOid.value = null;
    bindActivityType.value = '';
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

const TABLE_SCROLL_X = 1120;

const columns: DataTableColumns<Subcontractor> = [
  {
    title: 'Наименование', key: 'name', width: 280,
    ellipsis: { tooltip: true }
  },
  { title: 'БИН', key: 'bin', width: 160 },
  { title: 'Проектов', key: 'projectsCount', width: 100 },
  {
    title: 'Согласование', key: 'approval', width: 200,
    render: (row) => h(NSpace, { size: 4, wrap: false }, () => [
      h(NTag, { type: 'success', size: 'small', round: true }, () => `${row.employeesApprovedCount} согл.`),
      h(NTag, { type: 'warning', size: 'small', round: true }, () => `${row.employeesNotApprovedCount} ост.`)
    ])
  },
  {
    title: 'Действия', key: 'actions', width: 380,
    render: (row) => h(NSpace, { size: 'small', wrap: false }, () => [
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
  { title: 'OID', key: 'projectOid', width: 280, ellipsis: { tooltip: true } },
  { title: 'Название', key: 'name', width: 180, render: (row: ProjectBinding) => row.name || '—' },
  { title: 'Вид деятельности', key: 'activityType', width: 200, ellipsis: { tooltip: true } },
  {
    title: '% выполнения', key: 'completionPercent', width: 110,
    render: (row: ProjectBinding) => `${row.completionPercent}%`
  },
  {
    title: 'Отчёт', key: 'progressReportedAt', width: 140,
    render: (row: ProjectBinding) =>
      row.progressReportedAt ? new Date(row.progressReportedAt).toLocaleDateString('ru-RU') : '—'
  },
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
      <div class="t-table-wrap">
        <NDataTable
          class="t-data-table"
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(row) => row.id"
          :scroll-x="TABLE_SCROLL_X"
          size="small"
        />
      </div>
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

        <NSpace v-else vertical :size="12" style="width:100%">
          <NSpace align="center" style="width:100%">
            <NSelect
              v-model:value="selectedProjectOid"
              :options="availableProjectOptions"
              placeholder="Выберите проект для привязки"
              filterable
              clearable
              style="flex:1;min-width:280px"
              :disabled="bindingsLoading || !availableProjectOptions.length"
            />
            <NInput
              v-model:value="bindActivityType"
              placeholder="Вид деятельности на проекте"
              style="flex:1;min-width:240px"
              :disabled="bindingsLoading"
            />
            <NButton
              type="primary"
              :disabled="!selectedProjectOid || !bindActivityType.trim()"
              :loading="bindingSaving"
              @click="bindProject"
            >
              Привязать
            </NButton>
          </NSpace>
        </NSpace>

        <p v-if="projects.length && !availableProjectOptions.length && !bindingsLoading" style="color:var(--brand-text-muted);font-size:13px;margin:0">
          Все доступные проекты уже привязаны к этому субподрядчику.
        </p>

        <div class="t-table-wrap">
          <NDataTable
            class="t-data-table"
            :columns="bindingColumns"
            :data="bindings"
            :loading="bindingsLoading"
            :row-key="(r) => r.projectOid"
            :scroll-x="980"
            size="small"
          />
        </div>
      </NSpace>
    </NModal>
  </NCard>
</template>
