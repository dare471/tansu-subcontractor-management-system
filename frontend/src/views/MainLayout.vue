<script setup lang="ts">
import { computed, h } from 'vue';
import { useRouter, useRoute, RouterView } from 'vue-router';
import { NIcon, NAvatar, NDropdown } from 'naive-ui';
import {
  HomeOutline, PeopleOutline, BusinessOutline, PersonCircleOutline,
  GitNetworkOutline, IdCardOutline, MailUnreadOutline, TimeOutline,
  LogOutOutline, ChevronDownOutline, SettingsOutline,
  DocumentTextOutline, ClipboardOutline
} from '@vicons/ionicons5';
import { useAuthStore } from '@/stores/auth';
import type { TansuPermissions } from '@/api/auth';

const auth = useAuthStore();
const router = useRouter();
const route = useRoute();

type NavItem = {
  name: string;
  label: string;
  icon: any;
  roles?: ('TANSU' | 'Subcontractor')[];
  permission?: keyof TansuPermissions;
};

const allItems: NavItem[] = [
  { name: 'home', label: 'Главная', icon: HomeOutline },
  {
    name: 'subcontractors',
    label: 'Субподрядчики',
    icon: PeopleOutline,
    roles: ['TANSU'],
    permission: 'canViewEmployees'
  },
  { name: 'projects', label: 'Проекты', icon: BusinessOutline, roles: ['TANSU'] },
  {
    name: 'users',
    label: 'Пользователи',
    icon: PersonCircleOutline,
    roles: ['TANSU'],
    permission: 'canManageTansuUsers'
  },
  {
    name: 'tansu-employees',
    label: 'Сотрудники СП',
    icon: IdCardOutline,
    roles: ['TANSU'],
    permission: 'canViewEmployees'
  },
  {
    name: 'matrix',
    label: 'Матрица согласования',
    icon: GitNetworkOutline,
    roles: ['TANSU'],
    permission: 'canManageApprovalMatrix'
  },
  {
    name: 'document-matrix',
    label: 'Матрица заявок',
    icon: ClipboardOutline,
    roles: ['TANSU'],
    permission: 'canApproveEmployees'
  },
  {
    name: 'site-visit-journal',
    label: 'Журнал посещений',
    icon: TimeOutline,
    roles: ['TANSU'],
    permission: 'canViewVisitJournal'
  },
  { name: 'employees', label: 'Сотрудники', icon: IdCardOutline, roles: ['Subcontractor'] },
  { name: 'employee-batches', label: 'Пакеты согласования', icon: ClipboardOutline, roles: ['Subcontractor'] },
  { name: 'document-requests', label: 'Заявки', icon: DocumentTextOutline, roles: ['Subcontractor'] },
  {
    name: 'approvals-inbox',
    label: 'Согласование сотрудников',
    icon: MailUnreadOutline,
    roles: ['TANSU'],
    permission: 'canApproveEmployees'
  },
  { name: 'photo-reviews-inbox', label: 'Проверка фото', icon: IdCardOutline, roles: ['TANSU'] },
  {
    name: 'document-requests-inbox',
    label: 'Согласование заявок',
    icon: DocumentTextOutline,
    roles: ['TANSU'],
    permission: 'canApproveEmployees'
  }
];

const items = computed(() =>
  allItems.filter((i) => {
    if (i.roles && (!auth.user || !i.roles.includes(auth.user.userType))) return false;
    if (i.permission && !auth.permissions[i.permission] && !auth.permissions.isGlobalAdmin) return false;
    return true;
  })
);

const activeName = computed(() => route.name?.toString() ?? '');

function go(name: string) { router.push({ name }); }

const userDropdown = [
  { label: 'Личный кабинет', key: 'profile', icon: () => h(NIcon, null, () => h(SettingsOutline)) },
  { type: 'divider', key: 'd1' },
  { label: 'Выйти', key: 'logout', icon: () => h(NIcon, null, () => h(LogOutOutline)) }
];

function onUserAction(key: string) {
  if (key === 'profile') {
    router.push({ name: 'profile' });
    return;
  }
  if (key === 'logout') {
    auth.logout();
    router.push({ name: 'login' });
  }
}

const userInitials = computed(() => {
  const n = auth.user?.fullName ?? '';
  return n.split(/\s+/).filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase()).join('') || '?';
});

const roleLabels: Record<string, string> = {
  oid_manager: 'ОИД менеджер',
  oid_director: 'ОИД начальник',
  sb_project: 'СБ на проекте',
  sb_chief: 'СБ начальник',
  safety_project: 'БиОТ/ТБ на проекте',
  safety_chief: 'БиОТ начальник',
  project_manager: 'Руководитель проекта',
  global_admin: 'Глобальный администратор'
};

const userRoleLabel = computed(() => {
  if (auth.user?.userType === 'Subcontractor') return 'Субподрядчик';
  if (auth.user?.tansuRole) return roleLabels[auth.user.tansuRole] ?? auth.user.tansuRole;
  return 'Сотрудник ТАНСУ';
});
</script>

<template>
  <div style="display:flex;height:100vh;overflow:hidden">
    <aside class="t-sidebar" style="width:240px;flex-shrink:0">
      <div class="t-sidebar__brand">
        <div class="t-sidebar__logo">T</div>
        <div>
          <div class="t-sidebar__title">TANSU</div>
          <div class="t-sidebar__subtitle">Субподрядчики</div>
        </div>
      </div>

      <nav class="t-sidebar__nav">
        <div
          v-for="item in items"
          :key="item.name"
          class="t-sidebar__item"
          :class="{ 't-sidebar__item--active': activeName === item.name }"
          @click="go(item.name)"
        >
          <NIcon :component="item.icon" size="20" class="t-sidebar__icon" />
          <span>{{ item.label }}</span>
        </div>
      </nav>

      <div style="padding:14px 20px;border-top:1px solid rgba(255,255,255,.05);color:rgba(255,255,255,.4);font-size:11px">
        © Tansu — {{ new Date().getFullYear() }}
      </div>
    </aside>

    <div style="display:flex;flex-direction:column;flex:1;min-width:0">
      <header class="t-topbar">
        <div style="display:flex;align-items:center;gap:10px;min-width:200px">
          <span style="color:var(--brand-orange);font-weight:800;font-size:18px;letter-spacing:0.5px">TANSU</span>
          <span style="color:var(--brand-text-muted);font-size:11px;text-transform:uppercase">Субподрядчики</span>
        </div>
        <div style="flex:1"></div>

        <NDropdown trigger="click" :options="userDropdown" @select="onUserAction">
          <div class="t-topbar__user">
            <NAvatar
              round
              :size="36"
              :style="{ background: 'var(--brand-orange)', color:'#fff', fontWeight:700 }"
            >
              {{ userInitials }}
            </NAvatar>
            <div>
              <div class="t-topbar__user-name">{{ auth.user?.fullName ?? '—' }}</div>
              <div class="t-topbar__user-role">{{ userRoleLabel }}</div>
            </div>
            <NIcon :component="ChevronDownOutline" size="16" style="color:var(--brand-text-muted)" />
          </div>
        </NDropdown>
      </header>

      <main style="flex:1;overflow:auto;padding:24px;background:var(--brand-bg)">
        <RouterView />
      </main>
    </div>
  </div>
</template>
