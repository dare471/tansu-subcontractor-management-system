import { defineStore } from 'pinia';
import { authApi, type LoginResponse, type MeResponse } from '@/api/auth';
import { getToken, setToken } from '@/api/client';

type AuthState = {
  token: string | null;
  user: MeResponse | null;
  loading: boolean;
};

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: getToken(),
    user: null,
    loading: false
  }),
  getters: {
    isAuthenticated: (s) => !!s.token,
    mustChangePassword: (s) => !!s.user?.mustChangePassword
  },
  actions: {
    async login(iin: string, password: string) {
      this.loading = true;
      try {
        const res: LoginResponse = await authApi.login(iin, password);
        this.token = res.accessToken;
        setToken(res.accessToken);
        await this.fetchMe();
      } finally {
        this.loading = false;
      }
    },
    async changePassword(oldPassword: string, newPassword: string) {
      const res = await authApi.changePassword(oldPassword, newPassword);
      this.token = res.accessToken;
      setToken(res.accessToken);
      await this.fetchMe();
    },
    async fetchMe() {
      this.user = await authApi.me();
      if (this.user.userType !== 'Employee') {
        this.logout();
        throw new Error('Доступ только для сотрудников.');
      }
    },
    logout() {
      this.token = null;
      this.user = null;
      setToken(null);
    }
  }
});
