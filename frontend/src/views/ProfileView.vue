<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import {
  NCard, NSpace, NAvatar, NTag, NButton, NForm, NFormItem, NInput,
  NAlert, NDescriptions, NDescriptionsItem, NDataTable, NSelect, NDatePicker, useMessage
} from 'naive-ui';
import { useAuthStore } from '@/stores/auth';
import { authApi, type MyProject } from '@/api/auth';
import { delegationsApi, type ApproverDelegation } from '@/api/delegations';
import { usersApi } from '@/api/users';
import { approverRoleLabel } from '@/api/documentRequests';
import { toApiError } from '@/api/client';
import { appBrand } from '@/config/branding';

const auth = useAuthStore();
const msg = useMessage();

const projects = ref<MyProject[]>([]);
const projectsLoading = ref(false);
const delegations = ref<ApproverDelegation[]>([]);
const delegateUserId = ref<string | null>(null);
const delegateValidTo = ref<number | null>(null);
const tansuUsers = ref<{ label: string; value: string }[]>([]);

const oldPwd = ref('');
const newPwd = ref('');
const confirm = ref('');
const pwdSubmitting = ref(false);

const user = computed(() => auth.user);

const initials = computed(() => {
  const n = user.value?.fullName ?? '';
  return n.split(/\s+/).filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase()).join('') || '?';
});

const roleLabel = computed(() =>
  user.value?.userType === 'TANSU' ? appBrand.employeeLabel : 'Субподрядчик'
);

const approverRole = computed(() =>
  user.value?.approverRole ? approverRoleLabel(user.value.approverRole) : null
);

const pwdValid = computed(
  () =>
    newPwd.value.length >= 8 &&
    /[A-Z]/.test(newPwd.value) &&
    /[a-z]/.test(newPwd.value) &&
    /[0-9]/.test(newPwd.value) &&
    newPwd.value === confirm.value &&
    oldPwd.value.length > 0
);

const projectColumns = [
  { title: 'Проект', key: 'name', ellipsis: { tooltip: true }, render: (r: MyProject) => r.name || r.projectOid },
  {
    title: 'Матрица согласования',
    key: 'hasApprovalMatrix',
    width: 180,
    render: (r: MyProject) => r.hasApprovalMatrix ? 'Настроена' : 'Не настроена'
  }
];

async function loadProjects() {
  if (!auth.isSubcontractor) return;
  projectsLoading.value = true;
  try {
    projects.value = await authApi.myProjects();
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    projectsLoading.value = false;
  }
}

async function changePassword() {
  if (!pwdValid.value) {
    msg.warning('Проверьте требования к паролю и совпадение полей.');
    return;
  }
  pwdSubmitting.value = true;
  try {
    await auth.changePassword(oldPwd.value, newPwd.value);
    oldPwd.value = '';
    newPwd.value = '';
    confirm.value = '';
    msg.success('Пароль изменён');
  } catch (e) {
    msg.error(toApiError(e).detail);
  } finally {
    pwdSubmitting.value = false;
  }
}

async function loadDelegations() {
  if (!auth.isTansu || !auth.permissions.canApproveEmployees) return;
  try {
    delegations.value = await delegationsApi.list();
    const users = await usersApi.list();
    tansuUsers.value = users
      .filter((u) => u.userType === 'TANSU' && u.isActive)
      .map((u) => ({ label: u.fullName, value: u.id }));
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

async function createDelegation() {
  if (!delegateUserId.value || !delegateValidTo.value) return;
  try {
    await delegationsApi.create({
      delegateUserId: delegateUserId.value,
      validFrom: new Date().toISOString(),
      validTo: new Date(delegateValidTo.value).toISOString()
    });
    msg.success('Замещение создано');
    await loadDelegations();
  } catch (e) {
    msg.error(toApiError(e).detail);
  }
}

onMounted(async () => {
  await auth.fetchMe();
  await loadProjects();
  await loadDelegations();
});
</script>

<template>
  <NSpace vertical :size="16">
    <NCard title="Личный кабинет" :bordered="true">
      <div class="t-profile-header">
        <NAvatar
          round
          :size="72"
          :style="{ background: 'var(--brand-orange)', color: '#fff', fontWeight: 700, fontSize: '24px' }"
        >
          {{ initials }}
        </NAvatar>
        <div class="t-profile-header__info">
          <h2 class="t-profile-header__name">{{ user?.fullName ?? '—' }}</h2>
          <div class="t-profile-header__tags">
            <NTag :type="user?.userType === 'TANSU' ? 'info' : 'warning'" round>
              {{ roleLabel }}
            </NTag>
            <NTag v-if="approverRole" round>{{ approverRole }}</NTag>
            <NTag v-if="user?.mustChangePassword" type="error" round>Смените пароль</NTag>
          </div>
        </div>
      </div>

      <NDescriptions label-placement="left" :column="1" class="t-profile-desc" style="margin-top:20px">
        <NDescriptionsItem label="Email">{{ user?.email ?? '—' }}</NDescriptionsItem>
        <NDescriptionsItem label="Должность">{{ user?.position ?? '—' }}</NDescriptionsItem>
        <template v-if="auth.isSubcontractor">
          <NDescriptionsItem label="Организация">{{ user?.subcontractorName ?? '—' }}</NDescriptionsItem>
          <NDescriptionsItem label="БИН">{{ user?.subcontractorBin ?? '—' }}</NDescriptionsItem>
        </template>
      </NDescriptions>
    </NCard>

    <NCard
      v-if="auth.isTansu && auth.permissions.canApproveEmployees"
      title="Замещение при согласовании"
      :bordered="true"
    >
      <p style="margin-bottom:12px;color:var(--text-secondary)">
        Назначьте коллегу, который будет согласовывать заявки в ваше отсутствие.
      </p>
      <NSpace>
        <NSelect
          v-model:value="delegateUserId"
          :options="tansuUsers"
          placeholder="Замещающий"
          filterable
          style="min-width:240px"
        />
        <NDatePicker v-model:value="delegateValidTo" type="datetime" placeholder="Действует до" />
        <NButton type="primary" @click="createDelegation">Сохранить</NButton>
      </NSpace>
      <ul v-if="delegations.length" style="margin-top:16px">
        <li v-for="d in delegations" :key="d.id">
          {{ d.delegateName }} — до {{ new Date(d.validTo).toLocaleDateString('ru-RU') }}
        </li>
      </ul>
    </NCard>

    <NCard v-if="auth.isSubcontractor" title="Проекты организации" :bordered="true">
      <NDataTable
        class="t-data-table"
        :columns="projectColumns"
        :data="projects"
        :loading="projectsLoading"
        :row-key="(r) => r.projectOid"
        size="small"
      />
    </NCard>

    <NCard v-if="auth.isSubcontractor" title="Смена пароля" :bordered="true">
      <NAlert type="warning" :bordered="false" style="margin-bottom:16px">
        Пароль: не менее 8 символов, заглавная, строчная буква и цифра.
      </NAlert>
      <NForm @submit.prevent="changePassword" style="max-width:420px">
        <NFormItem label="Текущий пароль">
          <NInput v-model:value="oldPwd" type="password" show-password-on="click" />
        </NFormItem>
        <NFormItem label="Новый пароль">
          <NInput v-model:value="newPwd" type="password" show-password-on="click" />
        </NFormItem>
        <NFormItem label="Повторите новый">
          <NInput v-model:value="confirm" type="password" show-password-on="click" />
        </NFormItem>
        <NButton type="primary" :loading="pwdSubmitting" :disabled="!pwdValid" @click="changePassword">
          Сменить пароль
        </NButton>
      </NForm>
    </NCard>
  </NSpace>
</template>
