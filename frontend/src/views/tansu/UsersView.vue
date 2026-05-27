<script setup lang="ts">
import { ref, onMounted, h } from 'vue';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NModal, NForm, NFormItem,
  NSelect, NSwitch, NTag, useMessage, useDialog, type DataTableColumns
} from 'naive-ui';
import { usersApi, type User } from '@/api/users';
import { subcontractorsApi } from '@/api/subcontractors';
import { toApiError } from '@/api/client';

const msg = useMessage();
const dialog = useDialog();
const items = ref<User[]>([]);
const subs = ref<{ label: string; value: string }[]>([]);
const loading = ref(false);
const search = ref('');
const filterType = ref<string>('');

const showForm = ref(false);
const editing = ref<User | null>(null);
const form = ref({
  fullName: '',
  position: '',
  email: '',
  userType: 'Subcontractor' as 'TANSU' | 'Subcontractor',
  subcontractorId: null as string | null,
  isActive: true
});

async function load() {
  loading.value = true;
  try {
    items.value = await usersApi.list({
      userType: filterType.value || undefined,
      search: search.value || undefined
    });
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { loading.value = false; }
}

async function loadSubs() {
  const list = await subcontractorsApi.list();
  subs.value = list.map((s) => ({ label: `${s.name} (${s.bin})`, value: s.id }));
}

function openCreate() {
  editing.value = null;
  form.value = { fullName: '', position: '', email: '', userType: 'Subcontractor', subcontractorId: null, isActive: true };
  showForm.value = true;
}

function openEdit(row: User) {
  editing.value = row;
  form.value = {
    fullName: row.fullName, position: row.position, email: row.email,
    userType: row.userType, subcontractorId: row.subcontractorId, isActive: row.isActive
  };
  showForm.value = true;
}

async function save() {
  try {
    if (editing.value) {
      await usersApi.update(editing.value.id, form.value.fullName, form.value.position, form.value.isActive);
      msg.success('Сохранено');
    } else {
      const res = await usersApi.create(
        form.value.fullName, form.value.position, form.value.email,
        form.value.userType, form.value.subcontractorId);
      if (res.temporaryPassword) {
        dialog.info({
          title: 'Временный пароль создан',
          content: `Email: ${res.user.email}\nВременный пароль: ${res.temporaryPassword}\n\nТакже отправлен по email.`,
          positiveText: 'OK'
        });
      } else {
        msg.success('Пользователь создан');
      }
    }
    showForm.value = false;
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

async function resetPwd(row: User) {
  try {
    const r = await usersApi.resetPassword(row.id);
    dialog.info({
      title: 'Пароль сброшен',
      content: `Новый временный пароль для ${row.email}: ${r.temporaryPassword}\nТакже отправлен по email.`,
      positiveText: 'OK'
    });
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

const TABLE_SCROLL_X = 1390;

const columns: DataTableColumns<User> = [
  {
    title: 'ФИО', key: 'fullName', width: 200,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Email', key: 'email', width: 240,
    ellipsis: { tooltip: true }
  },
  {
    title: 'Должность', key: 'position', width: 180,
    ellipsis: { tooltip: true }
  },
  { title: 'Тип', key: 'userType', width: 130,
    render: (r) => h(NTag, { type: r.userType === 'TANSU' ? 'info' : 'success' }, () => r.userType) },
  {
    title: 'Субподрядчик', key: 'subcontractorName', width: 260,
    ellipsis: { tooltip: true }
  },
  { title: 'Активен', key: 'isActive', width: 100,
    render: (r) => h(NTag, { type: r.isActive ? 'success' : 'default' }, () => r.isActive ? 'Да' : 'Нет') },
  { title: 'Действия', key: 'actions', width: 280,
    render: (row) => h(NSpace, { size: 'small', wrap: false }, () => [
      h(NButton, { size: 'small', onClick: () => openEdit(row) }, () => 'Изменить'),
      row.userType === 'Subcontractor'
        ? h(NButton, { size: 'small', type: 'warning', onClick: () => resetPwd(row) }, () => 'Сбросить пароль')
        : null
    ])
  }
];

const typeOptions = [
  { label: 'Все', value: '' },
  { label: 'ТАНСУ', value: 'TANSU' },
  { label: 'Субподрядчик', value: 'Subcontractor' }
];

const userTypeOptions = [
  { label: 'ТАНСУ', value: 'TANSU' },
  { label: 'Субподрядчик', value: 'Subcontractor' }
];

onMounted(async () => { await Promise.all([load(), loadSubs()]); });
</script>

<template>
  <NCard title="Пользователи">
    <NSpace vertical>
      <NSpace>
        <NInput v-model:value="search" placeholder="Поиск" clearable style="width:280px" @keyup.enter="load" />
        <NSelect v-model:value="filterType" :options="typeOptions" style="width:180px" @update:value="load" />
        <NButton @click="load">Найти</NButton>
        <NButton type="primary" @click="openCreate">+ Новый</NButton>
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

    <NModal v-model:show="showForm" preset="card" :title="editing ? 'Изменить пользователя' : 'Новый пользователь'" style="width:520px">
      <NForm @submit.prevent="save">
        <NFormItem label="ФИО"><NInput v-model:value="form.fullName" /></NFormItem>
        <NFormItem label="Должность"><NInput v-model:value="form.position" /></NFormItem>
        <NFormItem label="Email">
          <NInput v-model:value="form.email" :disabled="!!editing" />
        </NFormItem>
        <NFormItem label="Тип пользователя" v-if="!editing">
          <NSelect v-model:value="form.userType" :options="userTypeOptions" />
        </NFormItem>
        <NFormItem label="Субподрядчик" v-if="!editing && form.userType === 'Subcontractor'">
          <NSelect v-model:value="form.subcontractorId" :options="subs" filterable />
        </NFormItem>
        <NFormItem label="Активен" v-if="editing">
          <NSwitch v-model:value="form.isActive" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="save">Сохранить</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </NCard>
</template>
