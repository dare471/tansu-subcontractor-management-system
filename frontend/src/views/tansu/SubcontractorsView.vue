<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue';
import { useRouter } from 'vue-router';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NForm, NFormItem,
  NPopconfirm, NSelect, NEmpty, NTag, NUpload, useMessage,
  type DataTableColumns, type SelectOption, type UploadFileInfo
} from 'naive-ui';
import { apiClient } from '@/api/client';
import {
  subcontractorsApi,
  SUBCONTRACTOR_DOC_TYPES,
  type Subcontractor,
  type ProjectBinding,
  type SubcontractorDocument
} from '@/api/subcontractors';
import { usersApi } from '@/api/users';
import { projectsApi, type Project } from '@/api/projects';
import { zupApi } from '@/api/zup';
import { toApiError } from '@/api/client';
import { useAuthStore } from '@/stores/auth';
import AppDrawer from '@/components/AppDrawer.vue';

const auth = useAuthStore();
const msg = useMessage();
const router = useRouter();
const items = ref<Subcontractor[]>([]);
const loading = ref(false);
const search = ref('');

const showForm = ref(false);
const editing = ref<Subcontractor | null>(null);
const form = ref({
  name: '',
  bin: '',
  managerUserId: null as string | null,
  projectOid: null as string | null,
  activityType: ''
});
const createProjectOptions = ref<SelectOption[]>([]);
const managerOptions = ref<SelectOption[]>([]);

const showDocs = ref(false);
const docsTarget = ref<Subcontractor | null>(null);
const docs = ref<SubcontractorDocument[]>([]);
const docsLoading = ref(false);
const docForm = ref({ name: '', documentType: 'contract' });
const docUploading = ref(false);
const docFile = ref<File | null>(null);
const docUploadKey = ref(0);

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

async function loadManagers() {
  if (!auth.permissions.canReassignSubcontractorManager && !auth.permissions.isGlobalAdmin) return;
  try {
    const users = await usersApi.list({ userType: 'TANSU' });
    managerOptions.value = users
      .filter((u) => u.isActive)
      .map((u) => ({ label: u.fullName, value: u.id }));
  } catch { managerOptions.value = []; }
}

async function loadCreateProjects() {
  try {
    let options = (await projectsApi.bindOptions()).map((p) => ({
      label: p.code ? `${p.code} — ${p.name ?? p.projectOid}` : (p.name || p.projectOid),
      value: p.projectOid
    }));
    if (!options.length) {
      const remote = await zupApi.projects();
      options = remote.map((p) => ({
        label: p.code ? `${p.code} — ${p.name ?? p.projectOid}` : (p.name || p.projectOid),
        value: p.projectOid
      }));
    }
    if (!options.length) {
      const all = await projectsApi.list();
      options = all.map((p) => ({
        label: p.code ? `${p.code} — ${p.name ?? p.projectOid}` : (p.name || p.projectOid),
        value: p.projectOid
      }));
    }
    createProjectOptions.value = options;
  } catch {
    createProjectOptions.value = [];
  }
}

function openCreate() {
  editing.value = null;
  form.value = { name: '', bin: '', managerUserId: null, projectOid: null, activityType: '' };
  showForm.value = true;
  void loadCreateProjects();
}

function openEdit(row: Subcontractor) {
  editing.value = row;
  form.value = {
    name: row.name,
    bin: row.bin,
    managerUserId: row.managerUserId,
    projectOid: null,
    activityType: ''
  };
  showForm.value = true;
}

async function save() {
  try {
    if (editing.value)
      await subcontractorsApi.update(editing.value.id, {
        name: form.value.name,
        bin: form.value.bin,
        managerUserId: form.value.managerUserId
      });
    else {
      const selected = createProjectOptions.value.find((o) => o.value === form.value.projectOid);
      await subcontractorsApi.create(form.value.name, form.value.bin, form.value.projectOid
        ? {
            projectOid: form.value.projectOid,
            projectName: typeof selected?.label === 'string' ? selected.label : undefined,
            activityType: form.value.activityType.trim()
          }
        : undefined);
    }
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

async function openDocuments(row: Subcontractor) {
  docsTarget.value = row;
  showDocs.value = true;
  docsLoading.value = true;
  try {
    docs.value = await subcontractorsApi.documents(row.id);
  } catch (e) {
    msg.error(toApiError(e).detail);
    docs.value = [];
  } finally {
    docsLoading.value = false;
  }
}

function onDocFileChange({ fileList }: { fileList: UploadFileInfo[] }) {
  const latest = fileList.at(-1);
  docFile.value = latest?.file instanceof File ? latest.file : null;
}

async function uploadDoc() {
  if (!docsTarget.value || !docForm.value.name.trim()) {
    msg.warning('Укажите наименование документа');
    return;
  }
  if (!docFile.value) {
    msg.warning('Выберите файл');
    return;
  }
  docUploading.value = true;
  try {
    const fd = new FormData();
    fd.append('name', docForm.value.name.trim());
    fd.append('documentType', docForm.value.documentType);
    fd.append('file', docFile.value);
    await subcontractorsApi.uploadDocument(docsTarget.value.id, fd);
    docs.value = await subcontractorsApi.documents(docsTarget.value.id);
    docForm.value.name = '';
    docFile.value = null;
    docUploadKey.value += 1;
    msg.success('Документ загружен');
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { docUploading.value = false; }
}

async function removeDoc(doc: SubcontractorDocument) {
  if (!docsTarget.value) return;
  try {
    await subcontractorsApi.deleteDocument(docsTarget.value.id, doc.id);
    docs.value = await subcontractorsApi.documents(docsTarget.value.id);
    msg.success('Удалено');
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function openDocFile(doc: SubcontractorDocument) {
  if (!docsTarget.value) return;
  try {
    const res = await apiClient.get(
      subcontractorsApi.documentUrl(docsTarget.value.id, doc.id),
      { responseType: 'blob' }
    );
    const url = URL.createObjectURL(res.data);
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  } catch (e) { msg.error(toApiError(e).detail); }
}

const TABLE_SCROLL_X = 1280;
const BINDING_TABLE_SCROLL_X = 1040;

const columns: DataTableColumns<Subcontractor> = [
  {
    title: 'Наименование', key: 'name', width: 280,
    ellipsis: { tooltip: true }
  },
  { title: 'БИН', key: 'bin', width: 160 },
  {
    title: 'Менеджер', key: 'managerFullName', width: 180,
    render: (row) => row.managerFullName ?? '—',
    ellipsis: { tooltip: true }
  },
  { title: 'Проектов', key: 'projectsCount', width: 90 },
  {
    title: 'Согласование', key: 'approval', width: 200,
    render: (row) => h(NSpace, { size: 4, wrap: false }, () => [
      h(NTag, { type: 'success', size: 'small', round: true }, () => `${row.employeesApprovedCount} согл.`),
      h(NTag, { type: 'warning', size: 'small', round: true }, () => `${row.employeesNotApprovedCount} ост.`)
    ])
  },
  {
    title: 'Действия', key: 'actions', width: 440,
    render: (row) => h(NSpace, { size: 'small', wrap: false }, () => [
      auth.canRegisterSubcontractors
        ? h(NButton, { size: 'small', onClick: () => openEdit(row) }, () => 'Изменить')
        : null,
      h(NButton, { size: 'small', onClick: () => openDocuments(row) }, () => 'Документы'),
      h(NButton, { size: 'small', onClick: () => openBindings(row) }, () => 'Проекты'),
      auth.canRegisterSubcontractors
        ? h(NPopconfirm, {
          onPositiveClick: () => remove(row)
        }, {
          default: () => 'Удалить субподрядчика?',
          trigger: () => h(NButton, { size: 'small', type: 'error' }, () => 'Удалить')
        })
        : null
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

onMounted(async () => {
  await Promise.all([load(), loadManagers()]);
});
</script>

<template>
  <NCard title="Субподрядчики">
    <NSpace vertical>
      <NSpace>
        <NInput v-model:value="search" placeholder="Поиск по названию или БИН" clearable @keyup.enter="load" style="width:300px" />
        <NButton @click="load">Найти</NButton>
        <NButton v-if="auth.canRegisterSubcontractors" type="primary" @click="openCreate">+ Новый</NButton>
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

    <AppDrawer v-model:show="showForm" :title="editing ? 'Изменить субподрядчика' : 'Новый субподрядчик'" width="narrow">
      <NForm @submit.prevent="save">
        <NFormItem label="Наименование">
          <NInput v-model:value="form.name" />
        </NFormItem>
        <NFormItem label="БИН">
          <NInput v-model:value="form.bin" />
        </NFormItem>
        <template v-if="!editing">
          <NFormItem label="Проект (опционально)">
            <NSelect
              v-model:value="form.projectOid"
              :options="createProjectOptions"
              placeholder="Выберите проект из справочника"
              filterable
              clearable
            />
          </NFormItem>
          <NFormItem v-if="form.projectOid" label="Вид деятельности на проекте">
            <NInput v-model:value="form.activityType" placeholder="Например: монтажные работы" />
          </NFormItem>
        </template>
        <NFormItem
          v-if="editing && (auth.permissions.canReassignSubcontractorManager || auth.permissions.isGlobalAdmin)"
          label="Менеджер"
        >
          <NSelect
            v-model:value="form.managerUserId"
            :options="managerOptions"
            filterable
            clearable
            placeholder="Ответственный менеджер"
          />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="save">Сохранить</NButton>
        </NSpace>
      </NForm>
    </AppDrawer>

    <AppDrawer
      v-model:show="showBindings"
      :title="'Проекты субподрядчика «' + (bindTarget?.name ?? '') + '»'"
      width="full"
    >
      <NEmpty
        v-if="!projects.length && !bindingsLoading"
        description="Нет зарегистрированных проектов"
      >
        <template #extra>
          <NButton type="primary" @click="router.push({ name: 'projects' })">
            Перейти к регистрации проектов
          </NButton>
        </template>
      </NEmpty>

      <NSpace
        v-if="projects.length && auth.canRegisterSubcontractors"
        class="bindings-form-row"
        align="center"
        :size="12"
        :wrap="true"
      >
        <NSelect
          v-model:value="selectedProjectOid"
          :options="availableProjectOptions"
          placeholder="Выберите проект для привязки"
          filterable
          clearable
          class="bindings-form-field bindings-form-field--project"
          :disabled="bindingsLoading || !availableProjectOptions.length"
        />
        <NInput
          v-model:value="bindActivityType"
          placeholder="Вид деятельности на проекте"
          class="bindings-form-field bindings-form-field--activity"
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

      <p
        v-if="projects.length && !availableProjectOptions.length && !bindingsLoading"
        class="bindings-hint"
      >
        Все доступные проекты уже привязаны к этому субподрядчику.
      </p>

      <div class="t-table-wrap">
        <NDataTable
          class="t-data-table"
          :columns="bindingColumns"
          :data="bindings"
          :loading="bindingsLoading"
          :row-key="(r) => r.projectOid"
          :scroll-x="BINDING_TABLE_SCROLL_X"
          size="small"
        />
      </div>
    </AppDrawer>

    <AppDrawer
      v-model:show="showDocs"
      :title="'Документы: ' + (docsTarget?.name ?? '')"
      width="wide"
    >
      <NSpace v-if="auth.canRegisterSubcontractors" align="center" :wrap="true">
        <NInput v-model:value="docForm.name" placeholder="Наименование" style="flex:1;min-width:200px" />
        <NSelect v-model:value="docForm.documentType" :options="SUBCONTRACTOR_DOC_TYPES" style="width:200px" />
        <NUpload
          :key="docUploadKey"
          accept=".pdf,.jpg,.jpeg,.png,.docx,.xlsx"
          :max="1"
          :show-file-list="false"
          :disabled="docUploading"
          class="sub-doc-upload"
          @change="onDocFileChange"
        >
          <NButton :disabled="docUploading">
            {{ docFile ? docFile.name : 'Выбрать файл' }}
          </NButton>
        </NUpload>
        <NButton type="primary" :loading="docUploading" @click="uploadDoc">Загрузить</NButton>
      </NSpace>
      <div class="t-table-wrap">
        <NDataTable
          class="t-data-table"
          :columns="[
            { title: 'Название', key: 'name' },
            { title: 'Тип', key: 'documentTypeLabel' },
            { title: 'Дата', key: 'uploadedAt', render: (r: SubcontractorDocument) => new Date(r.uploadedAt).toLocaleString('ru-RU') },
            { title: '', key: 'a', render: (r: SubcontractorDocument) => h(NSpace, { size: 'small' }, () => [
              h(NButton, { size: 'small', onClick: () => openDocFile(r) }, () => 'Открыть'),
              auth.canRegisterSubcontractors ? h(NButton, { size: 'small', type: 'error', onClick: () => removeDoc(r) }, () => 'Удалить') : null
            ]) }
          ]"
          :data="docs"
          :loading="docsLoading"
          :row-key="(r: SubcontractorDocument) => r.id"
          size="small"
        />
      </div>
    </AppDrawer>
  </NCard>
</template>

<style scoped>
.bindings-form-row {
  width: 100%;
}

.bindings-form-field {
  flex: 1 1 240px;
  min-width: 220px;
}

.bindings-form-field--project {
  flex: 1.2 1 280px;
  min-width: 260px;
}

.bindings-hint {
  margin: 0;
  font-size: 13px;
  color: var(--brand-text-muted);
}

.sub-doc-upload :deep(.n-upload-trigger) {
  display: inline-flex;
}

.sub-doc-upload :deep(.n-button) {
  max-width: 240px;
}

.sub-doc-upload :deep(.n-button__content) {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
