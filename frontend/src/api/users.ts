import { apiClient } from './client';

export type UserType = 'TANSU' | 'Subcontractor' | 'Employee';

export type User = {
  id: string;
  fullName: string;
  position: string;
  email: string;
  userType: UserType;
  subcontractorId: string | null;
  subcontractorName: string | null;
  employeeId: string | null;
  approverRole: string | null;
  tansuRole: string | null;
  managerUserId: string | null;
  projectOids: string[];
  projectNames: string[];
  subcontractorIds: string[];
  subcontractorNames: string[];
  mustChangePassword: boolean;
  isActive: boolean;
  blockReason: string | null;
  createdAt: string;
};

export type UserBlockRecord = {
  id: string;
  userId: string;
  initiatedByUserId: string;
  initiatedByFullName: string;
  actionType: 'block' | 'unblock';
  reason: string;
  createdAt: string;
};

export type UserBlockStatus = {
  isBlocked: boolean;
  lastRecord: UserBlockRecord | null;
  history: UserBlockRecord[];
};

export type CreateUserResponse = { user: User; temporaryPassword: string | null };

export type UpdateUserPayload = {
  fullName: string;
  position: string;
  isActive: boolean;
  statusComment?: string | null;
  approverRole?: string | null;
  tansuRole?: string | null;
  managerUserId?: string | null;
  projectOids?: string[];
  subcontractorIds?: string[];
};

export const USER_TYPE_LABELS: Record<UserType, string> = {
  TANSU: 'ТАНСУ',
  Subcontractor: 'Админ субподрядчика',
  Employee: 'Сотрудник (ЛК)'
};

export const usersApi = {
  list: (params: { userType?: string; subcontractorId?: string; search?: string } = {}) =>
    apiClient.get<User[]>('/api/users', { params }).then((r) => r.data),
  create: (payload: {
    fullName: string;
    position: string;
    email: string;
    userType: 'TANSU' | 'Subcontractor';
    subcontractorId?: string | null;
    tansuRole?: string | null;
    managerUserId?: string | null;
    projectOids?: string[];
    subcontractorIds?: string[];
  }) =>
    apiClient.post<CreateUserResponse>('/api/users', payload).then((r) => r.data),
  update: (id: string, payload: UpdateUserPayload) =>
    apiClient.put<User>(`/api/users/${id}`, payload).then((r) => r.data),
  blocks: (id: string) =>
    apiClient.get<UserBlockStatus>(`/api/users/${id}/blocks`).then((r) => r.data),
  resetPassword: (id: string) =>
    apiClient
      .post<{ temporaryPassword: string }>(`/api/users/${id}/reset-password`)
      .then((r) => r.data)
};

export const TANSU_ROLE_OPTIONS = [
  { label: 'ОИД менеджер', value: 'oid_manager' },
  { label: 'ОИД начальник / Ком. директор', value: 'oid_director' },
  { label: 'СБ на проекте', value: 'sb_project' },
  { label: 'СБ начальник', value: 'sb_chief' },
  { label: 'БиОТ/ТБ на проекте', value: 'safety_project' },
  { label: 'БиОТ начальник', value: 'safety_chief' },
  { label: 'Руководитель проекта', value: 'project_manager' },
  { label: 'Глобальный администратор', value: 'global_admin' }
];
