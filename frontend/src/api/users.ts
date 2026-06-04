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
  employerCompany: string | null;
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
  employerCompany?: string | null;
  managerUserId?: string | null;
};

export const USER_TYPE_LABELS: Record<UserType, string> = {
  TANSU: 'ТАНСУ',
  Subcontractor: 'Субподрядчик',
  Employee: 'Сотрудник (ЛК)'
};

export const TANSU_ROLE_OPTIONS = [
  { label: 'Менеджер', value: 'oid_manager' },
  { label: 'Администратор', value: 'oid_director' },
  { label: 'Согласующий (СБ на проекте)', value: 'sb_project' },
  { label: 'Согласующий (СБ начальник)', value: 'sb_chief' },
  { label: 'Согласующий (БиОТ на проекте)', value: 'safety_project' },
  { label: 'Согласующий (БиОТ начальник)', value: 'safety_chief' },
  { label: 'Согласующий (руководитель проекта)', value: 'project_manager' },
  { label: 'Глобальный администратор', value: 'global_admin' }
];

export const EMPLOYER_COMPANY_LABELS: Record<string, string> = {
  tansu_construction: 'ТОО TANSU Construction',
  kazprom_service: 'ТОО KazPromService'
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
    employerCompany?: string | null;
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
