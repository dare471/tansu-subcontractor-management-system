<script setup lang="ts">
import { ref, onMounted, h, computed, watch } from 'vue';
import {
  NCard, NSpace, NInput, NButton, NDataTable, NForm, NFormItem,
  NSelect, NSwitch, NTag, NAlert, NEmpty, useMessage, useDialog, type DataTableColumns
} from 'naive-ui';
import {
  usersApi,
  TANSU_ROLE_OPTIONS,
  TANSU_ROLES_NEEDING_PROJECTS,
  EMPLOYER_COMPANY_LABELS,
  USER_TYPE_LABELS,
  type User,
  type UserType,
  type UserBlockStatus
} from '@/api/users';
import { subcontractorsApi } from '@/api/subcontractors';
import { projectsApi, type Project } from '@/api/projects';
import { zupApi, TANSU_COMPANY_OPTIONS, type ZupEmployee } from '@/api/zup';
import { toApiError } from '@/api/client';
import { useAuthStore } from '@/stores/auth';
import AppDrawer from '@/components/AppDrawer.vue';

const auth = useAuthStore();
const msg = useMessage();
const dialog = useDialog();

const isGlobalAdmin = computed(
  () => !!auth.permissions.isGlobalAdmin || !!auth.permissions.canManageTansuUsers
);
const isManagerOnly = computed(
  () => auth.permissions.canManageSubcontractorUsers && !isGlobalAdmin.value
);

const items = ref<User[]>([]);
const subs = ref<{ label: string; value: string }[]>([]);
const projects = ref<Project[]>([]);
const tansuManagers = ref<{ label: string; value: string }[]>([]);
const loading = ref(false);
const search = ref('');
const filterType = ref(isManagerOnly.value ? 'Subcontractor' : 'TANSU');

const showForm = ref(false);
const editing = ref<User | null>(null);
const employeeMode = ref(false);
const form = ref({
  fullName: '',
  position: '',
  email: '',
  subcontractorId: null as string | null,
  tansuRole: null as string | null,
  employerCompany: null as string | null,
  zupEmployeeId: null as string | null,
  projectOids: [] as string[],
  subcontractorIds: [] as string[],
  isActive: true,
  statusComment: ''
});
const zupEmployees = ref<ZupEmployee[]>([]);
const zupLoading = ref(false);
const initialIsActive = ref(true);
const blockHistory = ref<UserBlockStatus | null>(null);
const showBlockModal = ref(false);
const blockTarget = ref<User | null>(null);
const blockComment = ref('');
const blockSubmitting = ref(false);

const needsBlockComment = computed(() =>
  !!editing.value && initialIsActive.value && !form.value.isActive
);

const filterHint = computed(() => {
  if (isManagerOnly.value)
    return 'Учётные записи администраторов организаций, которые вы зарегистрировали. Сначала создайте субподрядчика.';
  if (filterType.value === 'Subcontractor')
    return 'Учётные записи HR и администраторов организаций.';
  if (filterType.value === 'Employee')
    return 'Личные кабинеты сотрудников на объекте (создаются после согласования).';
  return 'Сотрудники ТАНСУ из ЗУП: выберите компанию, затем сотрудника и роль.';
});

const zupOptions = computed(() =>
  zupEmployees.value.map((e) => ({
    label: `${e.fullName} · ${e.position}${e.email ? ` · ${e.email}` : ''}`,
    value: e.externalId
  }))
);

const projectOptions = computed(() =>
  projects.value.map((p) => ({
    label: p.name || p.projectOid,
    value: p.projectOid
  }))
);

const createSubcontractorUser = computed(
  () => isManagerOnly.value || (isGlobalAdmin.value && filterType.value === 'Subcontractor')
);

const showVisibilityFields = computed(() => {
  if (!isGlobalAdmin.value || employeeMode.value || createSubcontractorUser.value) return false;
  if (editing.value) return editing.value.userType === 'TANSU';
  return true;
});

function tansuVisibilityPayload() {
  return {
    projectOids: form.value.projectOids,
    subcontractorIds: form.value.subcontractorIds
  };
}

function validateTansuVisibility(): boolean {
  if (!showVisibilityFields.value || !form.value.tansuRole) return true;
  if (TANSU_ROLES_NEEDING_PROJECTS.has(form.value.tansuRole) && form.value.projectOids.length === 0) {
    msg.warning('Для этой роли укажите хотя бы один проект.');
    return false;
  }
  return true;
}

watch(() => form.value.employerCompany, async (company) => {
  form.value.zupEmployeeId = null;
  zupEmployees.value = [];
  if (!company || !isGlobalAdmin.value || editing.value) return;
  zupLoading.value = true;
  try {
    zupEmployees.value = await zupApi.employees(company);
    if (!zupEmployees.value.length)
      msg.warning('Справочник ЗУП пуст или недоступен — заполните поля вручную.');
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    zupLoading.value = false;
  }
});

watch(() => form.value.zupEmployeeId, (id) => {
  if (!id) return;
  const row = zupEmployees.value.find((e) => e.externalId === id);
  if (!row) return;
  form.value.fullName = row.fullName;
  form.value.position = row.position;
  if (row.email) form.value.email = row.email;
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
    isGlobalAdmin.value ? projectsApi.list() : Promise.resolve([] as Project[])
  ]);
  subs.value = subList.map((s) => ({ label: `${s.name} (${s.bin})`, value: s.id }));
  projects.value = projectList;
  if (isGlobalAdmin.value) {
    const all = await usersApi.list({ userType: 'TANSU' });
    tansuManagers.value = all
      .filter((u) => u.isActive)
      .map((u) => ({ label: `${u.fullName} (${u.position})`, value: u.id }));
  }
}

function openCreate() {
  editing.value = null;
  employeeMode.value = false;
  form.value = {
    fullName: '', position: '', email: '',
    subcontractorId: null,
    tansuRole: isManagerOnly.value ? null : null,
    employerCompany: isGlobalAdmin.value ? null : null,
    zupEmployeeId: null,
    projectOids: [],
    subcontractorIds: [],
    isActive: true,
    statusComment: ''
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
      subcontractorId: row.subcontractorId,
      tansuRole: null, employerCompany: null, zupEmployeeId: null,
      projectOids: [], subcontractorIds: [],
      isActive: row.isActive, statusComment: ''
    };
    showForm.value = true;
    usersApi.blocks(row.id).then((h) => { blockHistory.value = h; }).catch(() => { blockHistory.value = null; });
    return;
  }

  employeeMode.value = false;
  editing.value = row;
  form.value = {
    fullName: row.fullName, position: row.position, email: row.email,
    subcontractorId: row.subcontractorId,
    tansuRole: row.tansuRole,
    employerCompany: row.employerCompany,
    zupEmployeeId: null,
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
      if (editing.value.userType === 'TANSU' && !validateTansuVisibility()) return;
      await usersApi.update(editing.value.id, {
        fullName: form.value.fullName,
        position: form.value.position,
        isActive: form.value.isActive,
        statusComment: statusChanged ? form.value.statusComment.trim() || null : null,
        tansuRole: editing.value.userType === 'TANSU' ? form.value.tansuRole : null,
        employerCompany: editing.value.userType === 'TANSU' ? form.value.employerCompany : null,
        ...(editing.value.userType === 'TANSU' ? tansuVisibilityPayload() : {})
      });
      msg.success('Сохранено');
    } else {
      if (!form.value.fullName.trim() || !form.value.position.trim() || !form.value.email.trim()) {
        msg.warning('Заполните ФИО, должность и email');
        return;
      }
      if (isGlobalAdmin.value && filterType.value === 'Subcontractor') {
        if (!form.value.subcontractorId) {
          msg.warning('Выберите организацию субподрядчика');
          return;
        }
        const res = await usersApi.create({
          fullName: form.value.fullName,
          position: form.value.position,
          email: form.value.email,
          userType: 'Subcontractor',
          subcontractorId: form.value.subcontractorId
        });
        dialog.info({
          title: 'Учётная запись создана',
          content: `Email: ${res.user.email}\nВременный пароль: ${res.temporaryPassword}`,
          positiveText: 'OK'
        });
      } else if (isGlobalAdmin.value) {
        if (!form.value.employerCompany) {
          msg.warning('Выберите компанию');
          return;
        }
        if (!form.value.tansuRole) {
          msg.warning('Выберите роль');
          return;
        }
        if (!validateTansuVisibility()) return;
        const res = await usersApi.create({
          fullName: form.value.fullName,
          position: form.value.position,
          email: form.value.email,
          userType: 'TANSU',
          tansuRole: form.value.tansuRole,
          employerCompany: form.value.employerCompany,
          ...tansuVisibilityPayload()
        });
        if (res.temporaryPassword) {
          dialog.info({
            title: 'Временный пароль',
            content: `Email: ${res.user.email}\nПароль: ${res.temporaryPassword}`,
            positiveText: 'OK'
          });
        } else msg.success('Пользователь создан');
      } else {
        if (!form.value.subcontractorId) {
          msg.warning('Выберите организацию субподрядчика');
          return;
        }
        const res = await usersApi.create({
          fullName: form.value.fullName,
          position: form.value.position,
          email: form.value.email,
          userType: 'Subcontractor',
          subcontractorId: form.value.subcontractorId
        });
        dialog.info({
          title: 'Учётная запись создана',
          content: `Email: ${res.user.email}\nВременный пароль: ${res.temporaryPassword}`,
          positiveText: 'OK'
        });
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

function companyLabel(code: string | null) {
  if (!code) return '—';
  return EMPLOYER_COMPANY_LABELS[code] ?? code;
}

function userTypeLabel(t: UserType) {
  return USER_TYPE_LABELS[t] ?? t;
}

function userTypeTagType(t: UserType) {
  if (t === 'TANSU') return 'info';
  if (t === 'Subcontractor') return 'warning';
  return 'default';
}

const TABLE_SCROLL_X = computed(() => {
  if (filterType.value === 'Subcontractor' || isManagerOnly.value) return 1200;
  if (filterType.value === 'Employee') return 1420;
  return isGlobalAdmin.value && filterType.value === 'TANSU' ? 1860 : 1500;
});

const columns = computed<DataTableColumns<User>>(() => {
  const base: DataTableColumns<User> = [
    { title: 'ФИО', key: 'fullName', width: 180, ellipsis: { tooltip: true } },
    { title: 'Email', key: 'email', width: 240, ellipsis: { tooltip: true } },
    { title: 'Должность', key: 'position', width: 160, ellipsis: { tooltip: true } }
  ];

  if (!filterType.value && isGlobalAdmin.value) {
    base.push({
      title: 'Тип', key: 'userType', width: 140,
      render: (r) => h(NTag, { type: userTypeTagType(r.userType), size: 'small' }, () => userTypeLabel(r.userType))
    });
  }

  if (filterType.value === 'Subcontractor' || isManagerOnly.value) {
    base.push({
      title: 'Организация', key: 'subcontractorName', width: 260,
      render: (r) => r.subcontractorName ?? '—',
      ellipsis: { tooltip: true }
    });
  }

  if (filterType.value === 'Employee') {
    base.push(
      {
        title: 'Работодатель', key: 'subcontractorName', width: 220,
        render: (r) => r.subcontractorName ?? '—',
        ellipsis: { tooltip: true }
      },
      {
        title: 'ID сотрудника', key: 'employeeId', width: 280,
        render: (r) => r.employeeId ?? '—',
        ellipsis: { tooltip: true }
      },
      {
        title: 'Причина блокировки', key: 'blockReason', width: 220,
        render: (r) => r.blockReason ?? '—',
        ellipsis: { tooltip: true }
      }
    );
  }

  if (filterType.value === 'TANSU' && isGlobalAdmin.value) {
    base.push(
      {
        title: 'Компания', key: 'employerCompany', width: 200,
        render: (r) => companyLabel(r.employerCompany)
      },
      {
        title: 'Роль', key: 'tansuRole', width: 220,
        render: (r) => roleLabel(r.tansuRole)
      },
      {
        title: 'Проекты', key: 'projectNames', width: 180,
        ellipsis: { tooltip: true },
        render: (r) => r.projectNames.length ? r.projectNames.join(', ') : '—'
      },
      {
        title: 'СП (огранич.)', key: 'subcontractorNames', width: 180,
        ellipsis: { tooltip: true },
        render: (r) => r.subcontractorNames.length ? r.subcontractorNames.join(', ') : '—'
      }
    );
  }

  base.push(
    {
      title: 'Статус', key: 'isActive', width: 100,
      render: (r) => h(NTag, { type: r.isActive ? 'success' : 'default' }, () => r.isActive ? 'Активен' : 'Отключён')
    },
    {
      title: 'Действия', key: 'actions', width: 300,
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

const typeOptions = computed(() => {
  if (isManagerOnly.value) return [{ label: 'Админы субподрядчиков', value: 'Subcontractor' }];
  return [
    { label: 'ТАНСУ', value: 'TANSU' },
    { label: 'Админы субподрядчиков', value: 'Subcontractor' },
    { label: 'Сотрудники (ЛК)', value: 'Employee' },
    { label: 'Все', value: '' }
  ];
});

const createModalTitle = computed(() => {
  if (employeeMode.value) return 'Сотрудник (личный кабинет)';
  if (editing.value) {
    if (editing.value.userType === 'Employee') return 'Сотрудник (личный кабинет)';
    if (editing.value.userType === 'Subcontractor') return 'Админ субподрядчика';
    return 'Пользователь ТАНСУ';
  }
  if (createSubcontractorUser.value) return 'Новый админ субподрядчика';
  return 'Новый пользователь ТАНСУ';
});

onMounted(async () => { await Promise.all([load(), loadFilters()]); });
</script>

<template>
  <NCard :title="isManagerOnly ? 'Пользователи субподрядчиков' : 'Пользователи'">
    <template #header-extra>
      <NTag v-if="isGlobalAdmin" type="info">Глобальный администратор</NTag>
      <NTag v-else-if="isManagerOnly" type="warning">Менеджер</NTag>
    </template>
    <NSpace vertical>
      <NSpace align="center">
        <NInput v-model:value="search" placeholder="Поиск по ФИО или email" clearable style="width:280px" @keyup.enter="load" />
        <NSelect
          v-if="!isManagerOnly"
          v-model:value="filterType"
          :options="typeOptions"
          style="width:220px"
          @update:value="load"
        />
        <NButton @click="load">Найти</NButton>
        <NButton type="primary" @click="openCreate">+ Пользователь</NButton>
      </NSpace>
      <p v-if="filterHint" style="margin:0;color:var(--brand-text-muted);font-size:13px">{{ filterHint }}</p>
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

    <AppDrawer v-model:show="showForm" :title="createModalTitle" width="wide">
      <NForm @submit.prevent="save">
        <template v-if="!editing && isGlobalAdmin && !employeeMode && !createSubcontractorUser">
          <NFormItem label="Компания">
            <NSelect
              v-model:value="form.employerCompany"
              :options="TANSU_COMPANY_OPTIONS"
              placeholder="ТОО TANSU Construction / KazPromService"
            />
          </NFormItem>
          <NFormItem label="Сотрудник из ЗУП">
            <NSelect
              v-model:value="form.zupEmployeeId"
              :options="zupOptions"
              :loading="zupLoading"
              filterable
              clearable
              placeholder="После выбора компании"
              :disabled="!form.employerCompany"
            />
          </NFormItem>
        </template>
        <NFormItem v-if="editing && form.employerCompany && isGlobalAdmin" label="Компания">
          <NInput :value="companyLabel(form.employerCompany)" disabled />
        </NFormItem>
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
        <p v-if="employeeMode" style="margin:0 0 12px;color:var(--brand-text-muted);font-size:13px">
          Учётная запись создаётся при согласовании сотрудника.
        </p>
        <NFormItem
          v-if="(!editing && createSubcontractorUser) && !employeeMode"
          label="Организация субподрядчика"
        >
          <NSelect
            v-model:value="form.subcontractorId"
            :options="subs"
            filterable
            placeholder="Сначала зарегистрируйте субподрядчика"
          />
        </NFormItem>
        <NFormItem
          v-if="isGlobalAdmin && !createSubcontractorUser && !employeeMode && (editing?.userType === 'TANSU' || !editing)"
          label="Роль"
        >
          <NSelect v-model:value="form.tansuRole" :options="TANSU_ROLE_OPTIONS" placeholder="Выберите роль" />
        </NFormItem>
        <template v-if="showVisibilityFields">
          <NFormItem label="Проекты">
            <NSelect
              v-model:value="form.projectOids"
              :options="projectOptions"
              multiple
              filterable
              clearable
              placeholder="Область видимости по проектам"
            />
          </NFormItem>
          <NFormItem label="Субподрядчики">
            <NSelect
              v-model:value="form.subcontractorIds"
              :options="subs"
              multiple
              filterable
              clearable
              placeholder="Дополнительное ограничение по СП"
            />
          </NFormItem>
          <p style="margin:0 0 12px;color:var(--brand-text-muted);font-size:13px">
            Для ролей на проекте (СБ/БиОТ/РП) укажите проекты. Субподрядчики — опционально, сужают список поверх роли.
          </p>
        </template>
        <NFormItem :label="employeeMode ? 'Доступ в ЛК' : 'Активен'" v-if="editing">
          <NSwitch v-model:value="form.isActive" />
        </NFormItem>
        <NFormItem v-if="needsBlockComment" label="Причина блокировки">
          <NInput v-model:value="form.statusComment" type="textarea" :rows="3" />
        </NFormItem>
        <NAlert v-if="employeeMode && editing && !editing.isActive && editing.blockReason" type="error" title="Заблокирован">
          {{ editing.blockReason }}
        </NAlert>
        <NSpace justify="end">
          <NButton @click="showForm = false">Отмена</NButton>
          <NButton type="primary" @click="save">Сохранить</NButton>
        </NSpace>
      </NForm>
    </AppDrawer>

    <AppDrawer
      v-model:show="showBlockModal"
      :title="blockTarget?.isActive ? 'Блокировка' : 'Разблокировка'"
      width="narrow"
    >
      <NSpace vertical>
        <NFormItem :label="blockTarget?.isActive ? 'Причина блокировки' : 'Комментарий'">
          <NInput v-model:value="blockComment" type="textarea" :rows="4" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="showBlockModal = false">Отмена</NButton>
          <NButton
            :type="blockTarget?.isActive ? 'error' : 'primary'"
            :loading="blockSubmitting"
            @click="submitBlockModal"
          >{{ blockTarget?.isActive ? 'Заблокировать' : 'Разблокировать' }}</NButton>
        </NSpace>
      </NSpace>
    </AppDrawer>
  </NCard>
</template>
