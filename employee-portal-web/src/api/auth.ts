import { apiClient } from './client';

export type LoginResponse = {
  accessToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  userType: string;
  mustChangePassword: boolean;
  employeeId?: string | null;
};

export type MeResponse = {
  id: string;
  fullName: string;
  email: string;
  position: string;
  userType: string;
  mustChangePassword: boolean;
  employeeId?: string | null;
};

export const authApi = {
  login: (iin: string, password: string) =>
    apiClient
      .post<LoginResponse>('/api/auth/employee/login', { iin, password })
      .then((r) => r.data),

  changePassword: (oldPassword: string, newPassword: string) =>
    apiClient
      .post<LoginResponse>('/api/auth/change-password', { oldPassword, newPassword })
      .then((r) => r.data),

  me: () => apiClient.get<MeResponse>('/api/auth/me').then((r) => r.data)
};
