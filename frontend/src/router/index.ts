import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/Login.vue'),
    meta: { public: true }
  },
  {
    path: '/change-password',
    name: 'change-password',
    component: () => import('@/views/auth/ForceChangePassword.vue'),
    meta: { allowMustChange: true }
  },
  {
    path: '/',
    component: () => import('@/views/MainLayout.vue'),
    children: [
      { path: '', redirect: { name: 'home' } },
      {
        path: 'home',
        name: 'home',
        component: () => import('@/views/Home.vue')
      },
      {
        path: 'profile',
        name: 'profile',
        component: () => import('@/views/ProfileView.vue')
      },
      {
        path: 'subcontractors',
        name: 'subcontractors',
        component: () => import('@/views/tansu/SubcontractorsView.vue'),
        meta: { roles: ['TANSU'] }
      },
      {
        path: 'projects',
        name: 'projects',
        component: () => import('@/views/tansu/ProjectsView.vue'),
        meta: { roles: ['TANSU'] }
      },
      {
        path: 'users',
        name: 'users',
        component: () => import('@/views/tansu/UsersView.vue'),
        meta: { roles: ['TANSU'] }
      },
      {
        path: 'matrix',
        name: 'matrix',
        component: () => import('@/views/tansu/MatrixView.vue'),
        meta: { roles: ['TANSU'] }
      },
      {
        path: 'document-matrix',
        name: 'document-matrix',
        component: () => import('@/views/tansu/DocumentMatrixView.vue'),
        meta: { roles: ['TANSU'] }
      },
      {
        path: 'employees',
        name: 'employees',
        component: () => import('@/views/subcontractor/EmployeesView.vue'),
        meta: { roles: ['Subcontractor'] }
      },
      {
        path: 'employee-batches',
        name: 'employee-batches',
        component: () => import('@/views/subcontractor/EmployeeBatchesView.vue'),
        meta: { roles: ['Subcontractor'] }
      },
      {
        path: 'document-requests',
        name: 'document-requests',
        component: () => import('@/views/subcontractor/DocumentRequestsView.vue'),
        meta: { roles: ['Subcontractor'] }
      },
      {
        path: 'employees/:id/approvals',
        name: 'employee-approvals',
        component: () => import('@/views/subcontractor/EmployeeApprovalsView.vue')
      },
      {
        path: 'approvals/inbox',
        name: 'approvals-inbox',
        component: () => import('@/views/approvals/InboxView.vue'),
        meta: { roles: ['TANSU'] }
      },
      {
        path: 'document-requests/inbox',
        name: 'document-requests-inbox',
        component: () => import('@/views/approvals/DocumentRequestsInboxView.vue'),
        meta: { roles: ['TANSU'] }
      }
    ]
  },
  { path: '/:pathMatch(.*)*', redirect: '/' }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach(async (to) => {
  const auth = useAuthStore();

  if (to.meta.public) return true;

  if (!auth.token) return { name: 'login', query: { redirect: to.fullPath } };

  if (!auth.user) {
    try {
      await auth.fetchMe();
    } catch {
      auth.logout();
      return { name: 'login' };
    }
  }

  if (auth.mustChangePassword && !to.meta.allowMustChange) {
    return { name: 'change-password' };
  }

  const roles = to.meta.roles as string[] | undefined;
  if (roles && auth.user && !roles.includes(auth.user.userType)) {
    return { name: 'home' };
  }

  return true;
});

export default router;
