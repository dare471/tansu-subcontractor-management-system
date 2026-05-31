<script setup lang="ts">
import { ref, onMounted, h, computed } from 'vue';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NModal, NForm, NFormItem,
  NSelect, NSwitch, NTag, useMessage, useDialog, type DataTableColumns
} from 'naive-ui';
import { usersApi, TANSU_ROLE_OPTIONS, USER_TYPE_LABELS, type User, type UserType } from '@/api/users';
import { subcontractorsApi } from '@/api/subcontractors';
import { projectsApi } from '@/api/projects';
import { toApiError } from '@/api/client';

const msg = useMessage();
const dialog = useDialog();
const items = ref<User[]>([]);
const subs = ref<{ label: string; value: string }[]>([]);
const projects = ref<{ label: string; value: string }[]>([]);
const loading = ref(false);
const search = ref('');
const filterType = ref<string>('TANSU');

const showForm = ref(false);
const editing = ref<User | null>(null);
const form = ref({
  fullName: '',
  position: '',
  email: '',
  userType: 'TANSU' as 'TANSU' | 'Subcontractor',
  subcontractorId: null as string | null,
  tansuRole: null as string | null,
  projectOids: [] as string[],
  subcontractorIds: [] as string[],
  isActive: true
});

const tansuUsers = computed(() => items.value.filter((u) => u.userType === 'TANSU'));

const filterHint = computed(() => {
  if (filterType.value === 'Subcontractor')
    return 'Учётные записи HR и администраторов организаций.';
  if (filterType.value === 'Employee')
    return 'Личные кабинеты сотрудников на объекте (создаются после согласования).';
  return '';
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

async function loadFilters() {
  const [subList, projectList] = await Promise.all([
    subcontractorsApi.list(),
    projectsApi.list()
  ]);
  subs.value = subList.map((s) => ({ label: `${s.name} (${s.bin})`, value: s.id }));
  projects.value = projectList.map((p) => ({
    label: p.name || p.projectOid,
    value: p.projectOid
  }));
}

function openCreate() {
  editing.value = null;
  form.value = {
    fullName: '', position: '', email: '',
    userType: 'TANSU', subcontractorId: null, tansuRole: null,
    projectOids: [], subcontractorIds: [], isActive: true
  };
  showForm.value = true;
}

function openEdit(row: User) {
  if (row.userType === 'Employee') {
    dialog.info({
      title: 'Сотрудник (личный кабинет)',
      content: `${row.fullName}. Учётная запись привязана к сотруднику и создаётся при согласовании. Организация: ${row.subcontractorName ?? '—'}.`,
      positiveText: 'OK'
    });
    return;
  }

  editing.value = row;
  form.value = {
    fullName: row.fullName, position: row.position, email: row.email,
    userType: row.userType as 'TANSU' | 'Subcontractor',
    subcontractorId: row.subcontractorId,
    tansuRole: row.tansuRole,
    projectOids: [...row.projectOids],
    subcontractorIds: [...row.subcontractorIds],
    isActive: row.isActive
  };
  showForm.value = true;
}

async function save() {
  try {
    if (editing.value) {
      await usersApi.update(editing.value.id, {
        fullName: form.value.fullName,
        position: form.value.position,
        isActive: form.value.isActive,
        tansuRole: form.value.userType === 'TANSU' ? form.value.tansuRole : null,
        projectOids: form.value.userType === 'TANSU' ? form.value.projectOids : [],
        subcontractorIds: form.value.userType === 'TANSU' ? form.value.subcontractorIds : []
      });
      msg.success('Сохранено');
    } else {
      const res = await usersApi.create({
        fullName: form.value.fullName,
        position: form.value.position,
        email: form.value.email,
        userType: form.value.userType,
        subcontractorId: form.value.subcontractorId,
        tansuRole: form.value.userType === 'TANSU' ? form.value.tansuRole : null,
        projectOids: form.value.userType === 'TANSU' ? form.value.projectOids : [],
        subcontractorIds: form.value.userType === 'TANSU' ? form.value.subcontractorIds : []
      });
      if (res.temporaryPassword) {
        dialog.info({
          title: 'Временный пароль создан',
          content: `Email: ${res.user.email}\nВременный пароль: ${res.temporaryPassword}`,
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
      content: `Новый временный пароль для ${row.email}: ${r.temporaryPassword}`,
      positiveText: 'OK'
    });
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
}

function roleLabel(role: string | null) {
  if (!role) return '—';
  return TANSU_ROLE_OPTIONS.find((o) => o.value === role)?.label ?? role;
}

function userTypeLabel(t: UserType) {
  return USER_TYPE_LABELS[t] ?? t;
}

function userTypeTagType(t: UserType) {
  if (t === 'TANSU') return 'info';
  if (t === 'Subcontractor') return 'warning';
  return 'default';
}

function employerLabel(r: User) {
  if (r.userType === 'Subcontractor' || r.userType === 'Employee')
    return r.subcontractorName ?? '—';
  return '—';
}

const TABLE_SCROLL_X = computed(() => {
  if (filterType.value === 'Subcontractor') return 1280;
  if (filterType.value === 'Employee') return 1200;
  if (filterType.value === 'TANSU') return 1680;
  return 1960;
});

const columns = computed<DataTableColumns<User>>(() => {
  const base: DataTableColumns<User> = [
    { title: 'ФИО', key: 'fullName', width: 180, ellipsis: { tooltip: true } },
    { title: 'Email', key: 'email', width: 240, ellipsis: { tooltip: true } },
    { title: 'Должность', key: 'position', width: 160, ellipsis: { tooltip: true } }
  ];

  if (!filterType.value) {
    base.push({
      title: 'Тип', key: 'userType', width: 170,
      render: (r) => h(NTag, { type: userTypeTagType(r.userType), size: 'small' }, () => userTypeLabel(r.userType))
    });
  }

  if (filterType.value !== 'TANSU') {
    base.push({
      title: filterType.value === 'Employee' ? 'Работодатель' : 'Организация',
      key: 'subcontractorName',
      width: 260,
      render: (r) => employerLabel(r),
      ellipsis: { tooltip: true }
    });
  }

  if (filterType.value === 'Employee') {
    base.push({
      title: 'ID сотрудника', key: 'employeeId', width: 280,
      render: (r) => r.employeeId ?? '—',
      ellipsis: { tooltip: true }
    });
  }

  if (filterType.value !== 'Subcontractor' && filterType.value !== 'Employee') {
    base.push({
      title: 'Роль ТАНСУ', key: 'tansuRole', width: 180,
      render: (r) => r.userType === 'TANSU' ? roleLabel(r.tansuRole) : '—'
    });
    base.push({
      title: 'Проекты (видимость)', key: 'projectNames', width: 220,
      render: (r) => r.userType === 'TANSU' && r.projectNames.length
        ? r.projectNames.join(', ')
        : '—',
      ellipsis: { tooltip: true }
    });
    base.push({
      title: 'Субподрядчики (видимость)', key: 'subcontractorNames', width: 240,
      render: (r) => r.userType === 'TANSU' && r.subcontractorNames.length
        ? r.subcontractorNames.join(', ')
        : '—',
      ellipsis: { tooltip: true }
    });
  }

  base.push(
    {
      title: 'Статус', key: 'isActive', width: 100,
      render: (r) => h(NTag, { type: r.isActive ? 'success' : 'default' }, () => r.isActive ? 'Активен' : 'Отключён')
    },
    {
      title: 'Действия', key: 'actions', width: 240,
      render: (row) => h(NSpace, { size: 'small', wrap: false }, () => [
        row.userType === 'Employee'
          ? h(NButton, { size: 'small', quaternary: true, onClick: () => openEdit(row) }, () => 'Сведения')
          : h(NButton, { size: 'small', onClick: () => openEdit(row) }, () => 'Изменить'),
        row.userType === 'Subcontractor'
          ? h(NButton, { size: 'small', type: 'warning', onClick: () => resetPwd(row) }, () => 'Сброс пароля')
          : null
      ])
    }
  );

  return base;
});

const typeOptions = [
  { label: 'ТАНСУ', value: 'TANSU' },
  { label: 'Админы субподрядчиков', value: 'Subcontractor' },
  { label: 'Сотрудники (ЛК)', value: 'Employee' },
  { label: 'Все', value: '' }
];

const userTypeOptions = [
  { label: 'ТАНСУ', value: 'TANSU' },
  { label: 'Админ субподрядчика', value: 'Subcontractor' }
];

onMounted(async () => { await Promise.all([load(), loadFilters()]); });
</script>

<template>
  <NCard title="Пользователи">
    <template #header-extra>
      <NTag type="info">Только глобальный администратор</NTag>
    </template>
    <NSpace vertical>
      <NSpace align="center">
        <NInput v-model:value="search" placeholder="Поиск по ФИО или email" clearable style="width:280px" @keyup.enter="load" />
        <NSelect v-model:value="filterType" :options="typeOptions" style="width:220px" @update:value="load" />
        <NButton @click="load">Найти</NButton>
        <NButton type="primary" @click="openCreate">+ Пользователь</NButton>
      </NSpace>
      <p v-if="filterHint" style="margin:0;color:var(--brand-text-muted);font-size:13px">{{ filterHint }}</p>
      <div class="t-table-wrap">
        <NDataTable
          class="t-data-table"
          :columns="columns"
          :data="filterType === 'TANSU' ? tansuUsers : items"
          :loading="loading"
          :row-key="(r) => r.id"
          :scroll-x="TABLE_SCROLL_X"
          size="small"
        />
      </div>
    </NSpace>

    <NModal
      v-model:show="showForm"
      preset="card"
      :title="editing
        ? (editing.userType === 'Subcontractor' ? 'Админ субподрядчика' : 'Пользователь ТАНСУ')
        : (form.userType === 'Subcontractor' ? 'Новый админ субподрядчика' : 'Новый пользователь ТАНСУ')"
      style="width:560px"
    >
      <NForm @submit.prevent="save">
        <NFormItem label="ФИО"><NInput v-model:value="form.fullName" /></NFormItem>
        <NFormItem label="Должность"><NInput v-model:value="form.position" /></NFormItem>
        <NFormItem label="Email">
          <NInput v-model:value="form.email" :disabled="!!editing" />
        </NFormItem>
        <NFormItem label="Тип пользователя" v-if="!editing">
          <NSelect v-model:value="form.userType" :options="userTypeOptions" />
        </NFormItem>
        <NFormItem
          v-if="form.userType === 'Subcontractor' && (!editing || editing.subcontractorName)"
          label="Организация"
        >
          <NSelect
            v-if="!editing"
            v-model:value="form.subcontractorId"
            :options="subs"
            filterable
            placeholder="Один субподрядчик на учётную запись"
          />
          <NInput v-else :value="editing?.subcontractorName ?? '—'" disabled />
        </NFormItem>
        <NFormItem label="Роль ТАНСУ" v-if="form.userType === 'TANSU'">
          <NSelect v-model:value="form.tansuRole" :options="TANSU_ROLE_OPTIONS" clearable placeholder="Выберите роль" />
        </NFormItem>
        <NFormItem label="Проекты (видимость)" v-if="form.userType === 'TANSU'">
          <NSelect
            v-model:value="form.projectOids"
            :options="projects"
            multiple
            filterable
            clearable
            placeholder="Пусто — ограничение по роли"
          />
        </NFormItem>
        <NFormItem label="Субподрядчики (видимость)" v-if="form.userType === 'TANSU'">
          <NSelect
            v-model:value="form.subcontractorIds"
            :options="subs"
            multiple
            filterable
            clearable
            placeholder="Ограничить область видимости"
          />
        </NFormItem>
        <NFormItem label="Активен" v-if="editing">
          <NSwitch v-model:value="form.isActive" />
          <span style="margin-left:8px;color:var(--brand-text-muted);font-size:12px">
            Снимите для деактивации учётной записи
          </span>
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="save">Сохранить</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </NCard>
</template>
