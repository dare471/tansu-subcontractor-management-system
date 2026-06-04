import { apiClient } from './client';

export type LoginResponse = {
  accessToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  userType: 'TANSU' | 'Subcontractor';
  subcontractorId: string | null;
  mustChangePassword: boolean;
  employeeId?: string | null;
};

export type TansuPermissions = {
  canRegisterSubcontractors: boolean;
  canManageApprovalMatrix: boolean;
  canApproveEmployees: boolean;
  canBlockEmployees: boolean;
  canViewVisitJournal: boolean;
  canManageTansuUsers: boolean;
  canManageSubcontractorUsers: boolean;
  canReassignSubcontractorManager: boolean;
  canManageSubordinates: boolean;
  canViewEmployees: boolean;
  canUploadDocuments: boolean;
  canViewProjects: boolean;
  canManageProjects: boolean;
  canViewSubcontractors: boolean;
  canReviewPhotos: boolean;
  isReadOnlyMonitoring: boolean;
  isGlobalAdmin: boolean;
};

export type MeResponse = {
  id: string;
  fullName: string;
  email: string;
  position: string;
  userType: 'TANSU' | 'Subcontractor';
  subcontractorId: string | null;
  subcontractorName: string | null;
  subcontractorBin: string | null;
  approverRole: string | null;
  mustChangePassword: boolean;
  employeeId?: string | null;
  isSuperUser: boolean;
  tansuRole: string | null;
  permissions: TansuPermissions | null;
};

export type MyProject = {
  projectOid: string;
  name: string | null;
  hasApprovalMatrix: boolean;
  activityType: string;
  completionPercent: number;
  progressReportedAt: string | null;
};

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<LoginResponse>('/api/auth/login', { email, password }).then((r) => r.data),

  devLogin: (email: string) =>
    apiClient.post<LoginResponse>('/api/auth/dev-login', { email }).then((r) => r.data),

  changePassword: (oldPassword: string, newPassword: string) =>
    apiClient
      .post<LoginResponse>('/api/auth/change-password', { oldPassword, newPassword })
      .then((r) => r.data),

  me: () => apiClient.get<MeResponse>('/api/auth/me').then((r) => r.data),

  myProjects: () => apiClient.get<MyProject[]>('/api/auth/me/projects').then((r) => r.data),

  reportProjectProgress: (projectOid: string, completionPercent: number) =>
    apiClient.put(`/api/auth/me/projects/${projectOid}/progress`, { completionPercent })
};
