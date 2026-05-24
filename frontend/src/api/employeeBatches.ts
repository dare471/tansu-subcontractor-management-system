import { apiClient } from './client';

export type ApprovalBatch = {
  id: string;
  title: string;
  status: 'draft' | 'submitted';
  projectOid: string;
  projectName: string | null;
  employeeCount: number;
  createdAt: string;
  submittedAt: string | null;
  employees: {
    employeeId: string;
    fullName: string;
    position: string;
    currentStatus: string | null;
  }[];
};

export type BatchSubmitResult = {
  batchId: string;
  title: string;
  submittedCount: number;
  items: { employeeId: string; roundId: string }[];
};

export const employeeBatchesApi = {
  list: () =>
    apiClient.get<ApprovalBatch[]>('/api/employee-batches').then((r) => r.data),

  get: (id: string) =>
    apiClient.get<ApprovalBatch>(`/api/employee-batches/${id}`).then((r) => r.data),

  create: (projectOid: string, title: string) =>
    apiClient
      .post<ApprovalBatch>('/api/employee-batches', { projectOid, title })
      .then((r) => r.data),

  addMembers: (batchId: string, employeeIds: string[]) =>
    apiClient
      .post<ApprovalBatch>(`/api/employee-batches/${batchId}/members`, { employeeIds })
      .then((r) => r.data),

  removeMember: (batchId: string, employeeId: string) =>
    apiClient
      .delete<ApprovalBatch>(`/api/employee-batches/${batchId}/members/${employeeId}`)
      .then((r) => r.data),

  submit: (batchId: string) =>
    apiClient
      .post<BatchSubmitResult>(`/api/employee-batches/${batchId}/submit`)
      .then((r) => r.data),

  remove: (batchId: string) =>
    apiClient.delete(`/api/employee-batches/${batchId}`)
};
