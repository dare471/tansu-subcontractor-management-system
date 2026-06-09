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
        meta: { roles: ['TANSU'], permission: 'canViewSubcontractors' }
      },
      {
        path: 'projects',
        name: 'projects',
        component: () => import('@/views/tansu/ProjectsView.vue'),
        meta: { roles: ['TANSU'], permission: 'canViewProjects' }
      },
      {
        path: 'projects/:projectOid',
        name: 'project-detail',
        component: () => import('@/views/tansu/ProjectDetailView.vue'),
        meta: { roles: ['TANSU'], permission: 'canViewProjects' }
      },
      {
        path: 'users',
        name: 'users',
        component: () => import('@/views/tansu/UsersView.vue'),
        meta: {
          roles: ['TANSU'],
          permissionAny: ['canManageTansuUsers', 'canManageSubcontractorUsers']
        }
      },
      {
        path: 'matrix',
        name: 'matrix',
        component: () => import('@/views/tansu/MatrixView.vue'),
        meta: { roles: ['TANSU'], permission: 'canManageApprovalMatrix' }
      },
      {
        path: 'document-matrix',
        name: 'document-matrix',
        component: () => import('@/views/tansu/DocumentMatrixView.vue'),
        meta: { roles: ['TANSU'], permission: 'canApproveEmployees' }
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
        path: 'project-progress',
        name: 'project-progress',
        component: () => import('@/views/subcontractor/ProjectProgressView.vue'),
        meta: { roles: ['Subcontractor'] }
      },
      {
        path: 'document-requests',
        name: 'document-requests',
        component: () => import('@/views/subcontractor/DocumentRequestsView.vue'),
        meta: { roles: ['Subcontractor'] }
      },
      {
        path: 'tansu/employees',
        name: 'tansu-employees',
        component: () => import('@/views/tansu/TansuEmployeesView.vue'),
        meta: { roles: ['TANSU'], permission: 'canViewEmployees' }
      },
      {
        path: 'employees/:id/approvals',
        name: 'employee-approvals',
        component: () => import('@/views/subcontractor/EmployeeApprovalsView.vue')
      },
      {
        path: 'site-visit-journal',
        name: 'site-visit-journal',
        component: () => import('@/views/tansu/SiteVisitJournalView.vue'),
        meta: { roles: ['TANSU'], permission: 'canViewVisitJournal' }
      },
      {
        path: 'audit-log',
        name: 'audit-log',
        component: () => import('@/views/tansu/AuditLogView.vue'),
        meta: { roles: ['TANSU'], permission: 'canViewAuditLog' }
      },
      {
        path: 'reports',
        name: 'reports',
        component: () => import('@/views/tansu/ReportsView.vue'),
        meta: { roles: ['TANSU', 'Subcontractor'], permission: 'canViewReports' }
      },
      {
        path: 'incidents',
        name: 'incidents',
        component: () => import('@/views/tansu/IncidentsView.vue'),
        meta: { roles: ['TANSU'], permission: 'canBlockEmployees' }
      },
      {
        path: 'photo-reviews/inbox',
        name: 'photo-reviews-inbox',
        component: () => import('@/views/tansu/PhotoReviewsInboxView.vue'),
        meta: { roles: ['TANSU'], permission: 'canReviewPhotos' }
      },
      {
        path: 'approvals/inbox',
        name: 'approvals-inbox',
        component: () => import('@/views/approvals/InboxView.vue'),
        meta: { roles: ['TANSU'], permission: 'canApproveEmployees' }
      },
      {
        path: 'document-requests/inbox',
        name: 'document-requests-inbox',
        component: () => import('@/views/approvals/DocumentRequestsInboxView.vue'),
        meta: { roles: ['TANSU'], permission: 'canApproveEmployees' }
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
    return { name: 'change-password', query: { redirect: to.fullPath } };
  }

  const roles = to.meta.roles as string[] | undefined;
  if (roles && auth.user && !roles.includes(auth.user.userType)) {
    return { name: 'home' };
  }

  const permission = to.meta.permission as keyof typeof auth.permissions | undefined;
  if (permission && !auth.permissions[permission] && !auth.permissions.isGlobalAdmin) {
    return { name: 'home' };
  }

  const permissionAny = to.meta.permissionAny as (keyof typeof auth.permissions)[] | undefined;
  if (permissionAny?.length) {
    const allowed = permissionAny.some((p) => auth.permissions[p]) || auth.permissions.isGlobalAdmin;
    if (!allowed) return { name: 'home' };
  }

  return true;
});

export default router;
