import { defineStore } from 'pinia';
import { authApi, type LoginResponse, type MeResponse } from '@/api/auth';

type AuthState = {
  token: string | null;
  user: MeResponse | null;
  loading: boolean;
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
      await authApi.changePassword(oldPassword, newPassword);
      await this.fetchMe();
    },
    async fetchMe() {
      this.user = await authApi.me();
    },
    logout() {
      this.token = null;
      this.user = null;
      localStorage.removeItem('tansu.token');
    }
  }
});
