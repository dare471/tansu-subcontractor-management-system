import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true }
  },
  {
    path: '/change-password',
    name: 'change-password',
    component: () => import('@/views/ChangePasswordView.vue'),
    meta: { allowMustChange: true }
  },
  {
    path: '/',
    component: () => import('@/views/LayoutView.vue'),
    children: [
      { path: '', name: 'dashboard', component: () => import('@/views/DashboardView.vue') },
      { path: 'approvals', name: 'approvals', component: () => import('@/views/ApprovalsView.vue') },
      { path: 'site-visits', name: 'site-visits', component: () => import('@/views/SiteVisitsView.vue') },
      { path: 'ppe', name: 'ppe', component: () => import('@/views/PpeView.vue') },
      { path: 'profile', name: 'profile', component: () => import('@/views/ProfileView.vue') },
      { path: 'quiz', name: 'quiz', component: () => import('@/views/SafetyQuizView.vue') }
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

  if (!auth.token) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }

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

  return true;
});

export default router;
