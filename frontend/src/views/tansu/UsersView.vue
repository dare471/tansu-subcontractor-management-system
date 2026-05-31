<script setup lang="ts">
import { ref, onMounted, h, computed } from 'vue';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NModal, NForm, NFormItem,
  NSelect, NSwitch, NTag, NAlert, NEmpty, useMessage, useDialog, type DataTableColumns
} from 'naive-ui';
import { usersApi, TANSU_ROLE_OPTIONS, USER_TYPE_LABELS, type User, type UserType, type UserBlockStatus } from '@/api/users';
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
const employeeMode = ref(false);
const form = ref({
  fullName: '',
  position: '',
  email: '',
  userType: 'TANSU' as 'TANSU' | 'Subcontractor',
  subcontractorId: null as string | null,
  tansuRole: null as string | null,
  projectOids: [] as string[],
  subcontractorIds: [] as string[],
  isActive: true,
  statusComment: ''
});
const initialIsActive = ref(true);
const blockHistory = ref<UserBlockStatus | null>(null);
const showBlockModal = ref(false);
const blockTarget = ref<User | null>(null);
const blockComment = ref('');
const blockSubmitting = ref(false);

const needsBlockComment = computed(() =>
  !!editing.value && initialIsActive.value && !form.value.isActive
);

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
  employeeMode.value = false;
  form.value = {
    fullName: '', position: '', email: '',
    userType: 'TANSU', subcontractorId: null, tansuRole: null,
    projectOids: [], subcontractorIds: [], isActive: true, statusComment: ''
  };
  showForm.value = true;
}

function openEdit(row: User) {
  blockHistory.value = null;
  initialIsActive.value = row.isActive;
  if (row.userType === 'Employee') {
    editing.value = row;
    employeeMode.value = true;
    form.value = {
      fullName: row.fullName, position: row.position, email: row.email,
      userType: 'Subcontractor',
      subcontractorId: row.subcontractorId,
      tansuRole: null,
      projectOids: [],
      subcontractorIds: [],
      isActive: row.isActive,
      statusComment: ''
    };
    showForm.value = true;
    usersApi.blocks(row.id).then((h) => { blockHistory.value = h; }).catch(() => { blockHistory.value = null; });
    return;
  }

  employeeMode.value = false;
  editing.value = row;
  form.value = {
    fullName: row.fullName, position: row.position, email: row.email,
    userType: row.userType as 'TANSU' | 'Subcontractor',
    subcontractorId: row.subcontractorId,
    tansuRole: row.tansuRole,
    projectOids: [...row.projectOids],
    subcontractorIds: [...row.subcontractorIds],
    isActive: row.isActive,
    statusComment: ''
  };
  showForm.value = true;
}

async function save() {
  try {
    if (editing.value) {
      if (needsBlockComment.value && form.value.statusComment.trim().length < 3) {
        msg.warning('Укажите причину блокировки (не короче 3 символов)');
        return;
      }
      const statusChanged = initialIsActive.value !== form.value.isActive;
      await usersApi.update(editing.value.id, {
        fullName: form.value.fullName,
        position: form.value.position,
        isActive: form.value.isActive,
        statusComment: statusChanged ? form.value.statusComment.trim() || null : null,
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

async function submitBlockModal() {
  if (!blockTarget.value) return;
  const blocking = blockTarget.value.isActive;
  if (blocking && blockComment.value.trim().length < 3) {
    msg.warning('Укажите причину блокировки (не короче 3 символов)');
    return;
  }
  blockSubmitting.value = true;
  try {
    await usersApi.update(blockTarget.value.id, {
      fullName: blockTarget.value.fullName,
      position: blockTarget.value.position,
      isActive: !blocking,
      statusComment: blockComment.value.trim() || null
    });
    showBlockModal.value = false;
    msg.success(blocking ? 'Учётная запись заблокирована' : 'Учётная запись разблокирована');
    await load();
  } catch (e) { msg.error(toApiError(e).detail); }
  finally { blockSubmitting.value = false; }
}

function openBlockModal(row: User) {
  blockTarget.value = row;
  blockComment.value = '';
  showBlockModal.value = true;
}

function blockActionLabel(action: string) {
  return action === 'block' ? 'Блокировка' : 'Разблокировка';
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
  if (filterType.value === 'Employee') return 1420;
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
    base.push({
      title: 'Причина блокировки', key: 'blockReason', width: 220,
      render: (r) => r.blockReason ?? '—',
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
      title: 'Действия', key: 'actions', width: 320,
      render: (row) => h(NSpace, { size: 'small', wrap: false }, () => [
        row.userType === 'Employee'
          ? h(NButton, {
            size: 'small',
            type: row.isActive ? 'error' : 'primary',
            onClick: () => openBlockModal(row)
          }, () => row.isActive ? 'Заблокировать' : 'Разблокировать')
          : h(NButton, { size: 'small', onClick: () => openEdit(row) }, () => 'Изменить'),
        row.userType === 'Employee'
          ? h(NButton, { size: 'small', quaternary: true, onClick: () => openEdit(row) }, () => 'Сведения')
          : null,
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
      :title="employeeMode
        ? 'Сотрудник (личный кабинет)'
        : (editing
          ? (editing.userType === 'Subcontractor' ? 'Админ субподрядчика' : 'Пользователь ТАНСУ')
          : (form.userType === 'Subcontractor' ? 'Новый админ субподрядчика' : 'Новый пользователь ТАНСУ'))"
      style="width:560px"
    >
      <NForm @submit.prevent="save">
        <NFormItem label="ФИО">
          <NInput v-model:value="form.fullName" :disabled="employeeMode" />
        </NFormItem>
        <NFormItem label="Должность">
          <NInput v-model:value="form.position" :disabled="employeeMode" />
        </NFormItem>
        <NFormItem label="Email">
          <NInput v-model:value="form.email" :disabled="!!editing || employeeMode" />
        </NFormItem>
        <NFormItem v-if="employeeMode && editing" label="Работодатель">
          <NInput :value="editing.subcontractorName ?? '—'" disabled />
        </NFormItem>
        <NFormItem v-if="employeeMode && editing?.employeeId" label="ID сотрудника">
          <NInput :value="editing.employeeId" disabled />
        </NFormItem>
        <p v-if="employeeMode" style="margin:0 0 12px;color:var(--brand-text-muted);font-size:13px">
          Учётная запись создаётся при согласовании сотрудника. Редактируются только статус доступа.
        </p>
        <NFormItem label="Тип пользователя" v-if="!editing && !employeeMode">
          <NSelect v-model:value="form.userType" :options="userTypeOptions" />
        </NFormItem>
        <NFormItem
          v-if="form.userType === 'Subcontractor' && (!editing || editing.subcontractorName) && !employeeMode"
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
        <NFormItem label="Роль ТАНСУ" v-if="form.userType === 'TANSU' && !employeeMode">
          <NSelect v-model:value="form.tansuRole" :options="TANSU_ROLE_OPTIONS" clearable placeholder="Выберите роль" />
        </NFormItem>
        <NFormItem label="Проекты (видимость)" v-if="form.userType === 'TANSU' && !employeeMode">
          <NSelect
            v-model:value="form.projectOids"
            :options="projects"
            multiple
            filterable
            clearable
            placeholder="Пусто — ограничение по роли"
          />
        </NFormItem>
        <NFormItem label="Субподрядчики (видимость)" v-if="form.userType === 'TANSU' && !employeeMode">
          <NSelect
            v-model:value="form.subcontractorIds"
            :options="subs"
            multiple
            filterable
            clearable
            placeholder="Ограничить область видимости"
          />
        </NFormItem>
        <NFormItem :label="employeeMode ? 'Доступ в ЛК' : 'Активен'" v-if="editing">
          <NSwitch v-model:value="form.isActive" />
          <span style="margin-left:8px;color:var(--brand-text-muted);font-size:12px">
            {{ employeeMode ? 'Снимите для блокировки входа в личный кабинет' : 'Снимите для деактивации учётной записи' }}
          </span>
        </NFormItem>
        <NFormItem v-if="needsBlockComment" label="Причина блокировки">
          <NInput
            v-model:value="form.statusComment"
            type="textarea"
            :rows="3"
            placeholder="Обязательно при блокировке"
          />
        </NFormItem>
        <NAlert v-if="employeeMode && editing && !editing.isActive && editing.blockReason" type="error" title="Заблокирован">
          {{ editing.blockReason }}
        </NAlert>
        <div v-if="employeeMode && blockHistory?.history.length" style="margin-bottom:12px">
          <div style="font-size:13px;font-weight:600;margin-bottom:8px">История блокировок</div>
          <NSpace vertical :size="8">
            <div
              v-for="item in blockHistory.history.slice(0, 5)"
              :key="item.id"
              style="padding:10px 12px;border:1px solid var(--brand-border);border-radius:8px;font-size:13px"
            >
              <div style="display:flex;justify-content:space-between;gap:12px;margin-bottom:4px">
                <NTag :type="item.actionType === 'block' ? 'error' : 'success'" size="small">
                  {{ blockActionLabel(item.actionType) }}
                </NTag>
                <span style="color:var(--brand-text-muted)">
                  {{ new Date(item.createdAt).toLocaleString('ru-RU') }}
                </span>
              </div>
              <div>{{ item.reason }}</div>
              <div style="color:var(--brand-text-muted);margin-top:4px">{{ item.initiatedByFullName }}</div>
            </div>
          </NSpace>
        </div>
        <NEmpty v-else-if="employeeMode && blockHistory && !blockHistory.history.length" description="История блокировок пуста" />
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="save">Сохранить</NButton>
        </NSpace>
      </NForm>
    </NModal>

    <NModal
      v-model:show="showBlockModal"
      preset="card"
      :title="blockTarget?.isActive ? 'Блокировка учётной записи' : 'Разблокировка учётной записи'"
      style="width:480px"
    >
      <NSpace vertical>
        <p style="margin:0;color:var(--brand-text-muted);font-size:13px">
          {{ blockTarget?.fullName }} · {{ blockTarget?.email }}
        </p>
        <NFormItem :label="blockTarget?.isActive ? 'Причина блокировки' : 'Комментарий'">
          <NInput
            v-model:value="blockComment"
            type="textarea"
            :rows="4"
            :placeholder="blockTarget?.isActive ? 'Обязательно (не короче 3 символов)' : 'Необязательно'"
          />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showBlockModal = false">Отмена</NButton>
          <NButton
            :type="blockTarget?.isActive ? 'error' : 'primary'"
            :loading="blockSubmitting"
            @click="submitBlockModal"
          >
            {{ blockTarget?.isActive ? 'Заблокировать' : 'Разблокировать' }}
          </NButton>
        </NSpace>
      </NSpace>
    </NModal>
  </NCard>
</template>
