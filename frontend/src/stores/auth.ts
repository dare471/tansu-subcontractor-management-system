import { defineStore } from 'pinia';
import { authApi, type LoginResponse, type MeResponse, type TansuPermissions } from '@/api/auth';
import { employeesApi } from '@/api/employees';

type AuthState = {
  token: string | null;
  user: MeResponse | null;
  loading: boolean;
};

const defaultPermissions: TansuPermissions = {
  canRegisterSubcontractors: false,
  canManageApprovalMatrix: false,
  canApproveEmployees: false,
  canBlockEmployees: false,
  canViewVisitJournal: false,
  canManageTansuUsers: false,
  canManageSubcontractorUsers: false,
  canReassignSubcontractorManager: false,
  canManageSubordinates: false,
  canViewEmployees: false,
  canUploadDocuments: false,
  canViewProjects: false,
  canManageProjects: false,
  canViewSubcontractors: false,
  canReviewPhotos: false,
  isReadOnlyMonitoring: false,
  isGlobalAdmin: false
};

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem('tansu.token'),
    user: null,
    loading: false
  }),
  getters: {
    isAuthenticated: (s) => !!s.token,
    isTansu: (s) => s.user?.userType === 'TANSU',
    isSubcontractor: (s) => s.user?.userType === 'Subcontractor',
    isSuperUser: (s) => !!s.user?.isSuperUser,
    permissions: (s): TansuPermissions => s.user?.permissions ?? defaultPermissions,
    canBlockEmployee: (s) => {
      if (!s.user || s.user.userType !== 'TANSU') return false;
      if (s.user.isSuperUser || s.user.permissions?.isGlobalAdmin) return true;
      return !!s.user.permissions?.canBlockEmployees;
    },
    canViewVisitJournal: (s) =>
      !!s.user?.isSuperUser || !!s.user?.permissions?.canViewVisitJournal,
    canManageUsers: (s) =>
      !!s.user?.permissions?.isGlobalAdmin ||
      !!s.user?.permissions?.canManageTansuUsers ||
      !!s.user?.isSuperUser,
    canManageSubcontractorUsers: (s) =>
      !!s.user?.permissions?.canManageSubcontractorUsers || !!s.user?.permissions?.isGlobalAdmin,
    canRegisterSubcontractors: (s) =>
      !!s.user?.permissions?.canRegisterSubcontractors || !!s.user?.permissions?.isGlobalAdmin,
    canManageApprovalMatrix: (s) =>
      !!s.user?.permissions?.canManageApprovalMatrix || !!s.user?.permissions?.isGlobalAdmin,
    canApproveEmployees: (s) =>
      !!s.user?.permissions?.canApproveEmployees || !!s.user?.permissions?.isGlobalAdmin,
    canViewEmployees: (s) =>
      s.user?.userType === 'Subcontractor' ||
      !!s.user?.permissions?.canViewEmployees ||
      !!s.user?.permissions?.isGlobalAdmin,
    isReadOnly: (s) =>
      !!s.user?.permissions?.isReadOnlyMonitoring && !s.user?.permissions?.isGlobalAdmin,
    canViewProjects: (s) =>
      !!s.user?.permissions?.canViewProjects || !!s.user?.permissions?.isGlobalAdmin,
    canManageProjects: (s) =>
      !!s.user?.permissions?.canManageProjects || !!s.user?.permissions?.isGlobalAdmin,
    canViewSubcontractors: (s) =>
      !!s.user?.permissions?.canViewSubcontractors || !!s.user?.permissions?.isGlobalAdmin,
    canReviewPhotos: (s) =>
      !!s.user?.permissions?.canReviewPhotos || !!s.user?.permissions?.isGlobalAdmin,
    mustChangePassword: (s) => !!s.user?.mustChangePassword
  },
  actions: {
    async login(email: string, password: string) {
      this.loading = true;
      try {
        const res: LoginResponse = await authApi.login(email, password);
        this.token = res.accessToken;
        localStorage.setItem('tansu.token', res.accessToken);
        await this.fetchMe();
      } finally {
        this.loading = false;
      }
    },
    async devLogin(email: string) {
      this.loading = true;
      try {
        const res: LoginResponse = await authApi.devLogin(email);
        this.token = res.accessToken;
        localStorage.setItem('tansu.token', res.accessToken);
        await this.fetchMe();
      } finally {
        this.loading = false;
      }
    },
    async changePassword(oldPassword: string, newPassword: string) {
      const res = await authApi.changePassword(oldPassword, newPassword);
      this.token = res.accessToken;
      localStorage.setItem('tansu.token', res.accessToken);
      await this.fetchMe();
    },
    async fetchMe() {
      this.user = await authApi.me();
    },
    logout() {
      employeesApi.clearPhotoCache();
      this.token = null;
      this.user = null;
      localStorage.removeItem('tansu.token');
    }
  }
});
