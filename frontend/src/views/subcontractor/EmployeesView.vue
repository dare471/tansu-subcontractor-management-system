<script setup lang="ts">
import { ref, onMounted, computed, h } from 'vue';
import { useRouter } from 'vue-router';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NModal, NForm, NFormItem,
  NSelect, NPopconfirm, NTag, NAlert, NEllipsis, useMessage,
  type DataTableColumns
} from 'naive-ui';
import { employeesApi, type Employee } from '@/api/employees';
import EmployeePhotoAvatar from '@/components/EmployeePhotoAvatar.vue';
import { authApi, type MyProject } from '@/api/auth';
import { useAuthStore } from '@/stores/auth';
import { toApiError } from '@/api/client';

const msg = useMessage();
const router = useRouter();
const auth = useAuthStore();

const items = ref<Employee[]>([]);
const loading = ref(false);
const search = ref('');
const projects = ref<MyProject[]>([]);
const projectsLoading = ref(false);

const showForm = ref(false);
const editing = ref<Employee | null>(null);
const form = ref({ projectOid: '', fullName: '', position: '', phone: '', iin: '' });

const photoUploadFor = ref<Employee | null>(null);

async function load() {
  loading.value = true;
  try { items.value = await employeesApi.list({ search: search.value || undefined }); }
  catch (e) { msg.error(toApiError(e).detail); }
  finally { loading.value = false; }
}

async function loadProjects() {
  if (!auth.user?.subcontractorId) {
    projects.value = [];
    return;
  }
  projectsLoading.value = true;
  try {
    projects.value = await authApi.myProjects();
  } catch (e) {
    projects.value = [];
    msg.error(toApiError(e).detail);
  } finally {
    projectsLoading.value = false;
  }
}

function openCreate() {
  editing.value = null;
  form.value = {
    projectOid: creatableProjects.value[0]?.projectOid ?? projects.value[0]?.projectOid ?? '',
    fullName: '', position: '', phone: '', iin: ''
  };
  showForm.value = true;
}

function openEdit(row: Employee) {
  editing.value = row;
  form.value = {
    projectOid: row.projectOid, fullName: row.fullName,
    position: row.position, phone: row.phone, iin: row.iin
  };
  showForm.value = true;
}

async function save() {
  try {
    if (editing.value) {
      await employeesApi.update(editing.value.id, form.value.fullName, form.value.position, form.value.phone, form.value.iin);
    } else {
      await employeesApi.create(form.value.projectOid, form.value.fullName, form.value.position, form.value.phone, form.value.iin);
    }
    msg.success('Сохранено');
    showForm.value = false;
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function remove(row: Employee) {
  try { await employeesApi.remove(row.id); msg.success('Удалено'); await load(); }
  catch (e) { msg.error(toApiError(e).detail); }
}

async function uploadPhoto(file: File) {
  if (!photoUploadFor.value) return;
  try {
    await employeesApi.uploadPhoto(photoUploadFor.value.id, file);
    employeesApi.invalidatePhotoCache(photoUploadFor.value.id);
    msg.success('Фото загружено');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function submit(row: Employee) {
  try {
    await employeesApi.submit(row.id);
    msg.success('Отправлено на согласование');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function resubmit(row: Employee) {
  try {
    await employeesApi.resubmit(row.id);
    msg.success('Повторно отправлено на согласование');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

function statusTag(status: string | null) {
  if (!status) return h(NTag, {}, () => 'Черновик');
  const type = status === 'approved' ? 'success'
    : status === 'rejected' ? 'error'
    : status === 'pending' ? 'warning'
    : 'default';
  const label = status === 'approved' ? 'Согласован'
    : status === 'rejected' ? 'Отклонён'
    : status === 'pending' ? 'На согласовании'
    : status;
  return h(NTag, { type }, () => label);
}

function hasApprovalMatrix(projectOid: string) {
  return projects.value.find((p) => p.projectOid === projectOid)?.hasApprovalMatrix ?? false;
}

const projectsWithoutMatrix = computed(() =>
  projects.value.filter((p) => !p.hasApprovalMatrix).map((p) => p.name || p.projectOid)
);

function renderSubmitButton(row: Employee) {
  if (row.draftBatchId) {
    return h(NButton, { size: 'small', disabled: true, title: 'Сотрудник в черновике пакета' }, () => 'В пакете');
  }
  if (row.currentStatus !== 'rejected' && row.currentStatus !== null) return null;

  const label = row.currentStatus === 'rejected' ? 'Повторно' : 'На согласование';
  if (!hasApprovalMatrix(row.projectOid)) {
    return h(
      NButton,
      { size: 'small', type: 'primary', disabled: true, title: 'Для проекта не настроена матрица согласования ТАНСУ' },
      () => label
    );
  }

  return h(NPopconfirm, {
    onPositiveClick: () => (row.currentStatus === 'rejected' ? resubmit(row) : submit(row))
  }, {
    default: () => (row.currentStatus === 'rejected' ? 'Повторно отправить?' : 'Отправить на согласование?'),
    trigger: () => h(NButton, { size: 'small', type: 'primary' }, () => label)
  });
}

const TABLE_SCROLL_X = 1640;

const columns: DataTableColumns<Employee> = [
  {
    title: 'Фото', key: 'photo', width: 72, align: 'center',
    render: (row) => h(EmployeePhotoAvatar, {
      employeeId: row.id,
      fullName: row.fullName,
      photoPath: row.photoPath,
      size: 40
    })
  },
  {
    title: 'ФИО', key: 'fullName', width: 200,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Должность', key: 'position', width: 180,
    ellipsis: { tooltip: true }
  },
  { title: 'ИИН', key: 'iin', width: 130 },
  { title: 'Телефон', key: 'phone', width: 150 },
  {
    title: 'Проект', key: 'projectName', width: 240,
    ellipsis: { tooltip: true }
  },
  { title: 'Статус', key: 'currentStatus', width: 150, render: (r) => statusTag(r.currentStatus) },
  {
    title: 'Пакет', key: 'batch', width: 200,
    render: (r) => {
      const title = r.draftBatchTitle ?? r.submittedBatchTitle;
      if (!title) return '—';
      return h(NTag, {
        size: 'small',
        type: r.draftBatchTitle ? 'default' : 'warning',
        style: { maxWidth: '100%' }
      }, () => h(NEllipsis, { style: { maxWidth: '180px' } }, () => title));
    }
  },
  {
    title: 'Действия', key: 'actions', width: 420,
    render: (row) => h(NSpace, { size: 'small', wrap: false }, () => [
      h(NButton, { size: 'small', onClick: () => openEdit(row) }, () => 'Изменить'),
      h(NButton, { size: 'small', onClick: () => { photoUploadFor.value = row; (document.getElementById('photo-input') as HTMLInputElement)?.click(); } }, () => 'Фото'),
      renderSubmitButton(row),
      h(NButton, { size: 'small', onClick: () => router.push({ name: 'employee-approvals', params: { id: row.id } }) }, () => 'История'),
      h(NPopconfirm, { onPositiveClick: () => remove(row) }, {
        default: () => 'Удалить сотрудника?',
        trigger: () => h(NButton, { size: 'small', type: 'error' }, () => 'Удалить')
      })
    ])
  }
];

const projectOptions = computed(() =>
  projects.value.map((p) => ({
    label: p.hasApprovalMatrix
      ? (p.name || p.projectOid)
      : `${p.name || p.projectOid} (матрица не настроена)`,
    value: p.projectOid,
    disabled: !p.hasApprovalMatrix
  }))
);

const creatableProjects = computed(() => projects.value.filter((p) => p.hasApprovalMatrix));

onMounted(async () => {
  await Promise.all([load(), loadProjects()]);
});

function onPhotoInputChange(event: Event) {
  const target = event.target as HTMLInputElement;
  const file = target?.files?.[0];
  if (file) uploadPhoto(file);
  target.value = '';
}
</script>

<template>
  <NCard title="Сотрудники">
    <NSpace vertical>
      <NAlert v-if="!projectsLoading && projectsWithoutMatrix.length" type="warning">
        Для проектов «{{ projectsWithoutMatrix.join('», «') }}» не настроена матрица согласования.
      </NAlert>
      <NAlert v-if="!projectsLoading && !projects.length" type="warning">
        Нет привязанных проектов.
      </NAlert>
      <NSpace>
        <NInput v-model:value="search" placeholder="Поиск" clearable style="width:280px" @keyup.enter="load" />
        <NButton @click="load">Найти</NButton>
        <NButton @click="router.push({ name: 'employee-batches' })">Пакеты</NButton>
        <NButton type="primary" :disabled="projectsLoading || !projects.length" :loading="projectsLoading" @click="openCreate">
          + Новый сотрудник
        </NButton>
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
    </NSpace>

    <input id="photo-input" type="file" accept="image/*" hidden @change="onPhotoInputChange" />

    <NModal v-model:show="showForm" preset="card" :title="editing ? 'Изменить сотрудника' : 'Новый сотрудник'" style="width:560px">
      <NForm @submit.prevent="save">
        <NFormItem label="Проект" v-if="!editing">
          <NSelect v-model:value="form.projectOid" :options="projectOptions" />
        </NFormItem>
        <NFormItem label="ФИО"><NInput v-model:value="form.fullName" /></NFormItem>
        <NFormItem label="Должность"><NInput v-model:value="form.position" /></NFormItem>
        <NFormItem label="Телефон"><NInput v-model:value="form.phone" /></NFormItem>
        <NFormItem label="ИИН (12 цифр)"><NInput v-model:value="form.iin" /></NFormItem>
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="save">Сохранить</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </NCard>
</template>
