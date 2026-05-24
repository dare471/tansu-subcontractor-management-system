import { apiClient } from './client';

export type User = {
  id: string;
  fullName: string;
  position: string;
  email: string;
  userType: 'TANSU' | 'Subcontractor';
  subcontractorId: string | null;
  subcontractorName: string | null;
  mustChangePassword: boolean;
  isActive: boolean;
  createdAt: string;
};

export type CreateUserResponse = { user: User; temporaryPassword: string | null };

export const usersApi = {
  list: (params: { userType?: string; subcontractorId?: string; search?: string } = {}) =>
    apiClient.get<User[]>('/api/users', { params }).then((r) => r.data),
  create: (
    fullName: string,
    position: string,
    email: string,
    userType: 'TANSU' | 'Subcontractor',
    subcontractorId?: string | null
  ) =>
    apiClient
      .post<CreateUserResponse>('/api/users', { fullName, position, email, userType, subcontractorId })
      .then((r) => r.data),
  update: (id: string, fullName: string, position: string, isActive: boolean) =>
    apiClient.put<User>(`/api/users/${id}`, { fullName, position, isActive }).then((r) => r.data),
  resetPassword: (id: string) =>
    apiClient
      .post<{ temporaryPassword: string }>(`/api/users/${id}/reset-password`)
      .then((r) => r.data)
};
