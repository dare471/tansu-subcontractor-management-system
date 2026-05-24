import { apiClient } from './client';

export type DocumentRequest = {
  id: string;
  subcontractorId: string;
  subcontractorName: string;
  projectOid: string;
  projectName: string | null;
  requestType: string;
  title: string;
  description: string;
  currentStatus: string | null;
  pendingApproverFullName: string | null;
  pendingApproverRole: string | null;
  pendingStepNo: number | null;
  createdAt: string;
  updatedAt: string;
};

export type DocumentRequestInboxItem = {
  sheetId: string;
  requestId: string;
  requestType: string;
  title: string;
  subcontractorName: string;
  projectOid: string;
  projectName: string | null;
  approverRole: string;
  orderNo: number;
  submittedAt: string;
};

export const REQUEST_TYPES = [
  { value: 'leave', label: 'Отпуск' },
  { value: 'ticket', label: 'Тикет / обращение' },
  { value: 'document', label: 'Документ' },
  { value: 'expense', label: 'Расход / финансы' }
] as const;

export const APPROVER_ROLES = [
  { value: 'accounting', label: 'Бухгалтерия' },
  { value: 'hr', label: 'Кадры' },
  { value: 'finance', label: 'Финансы' },
  { value: 'management', label: 'Руководство' }
] as const;

export function requestTypeLabel(type: string) {
  return REQUEST_TYPES.find((t) => t.value === type)?.label ?? type;
}

export function approverRoleLabel(role: string) {
  return APPROVER_ROLES.find((r) => r.value === role)?.label ?? role;
}

export const documentRequestsApi = {
  list: (params: { requestType?: string; search?: string } = {}) =>
    apiClient.get<DocumentRequest[]>('/api/document-requests', { params }).then((r) => r.data),

  create: (projectOid: string, requestType: string, title: string, description: string) =>
    apiClient
      .post<DocumentRequest>('/api/document-requests', { projectOid, requestType, title, description })
      .then((r) => r.data),

  update: (id: string, title: string, description: string) =>
    apiClient.put<DocumentRequest>(`/api/document-requests/${id}`, { title, description }).then((r) => r.data),

  remove: (id: string) => apiClient.delete(`/api/document-requests/${id}`),

  submit: (id: string) =>
    apiClient.post<{ roundId: string }>(`/api/document-requests/${id}/submit`).then((r) => r.data),

  resubmit: (id: string) =>
    apiClient.post<{ roundId: string }>(`/api/document-requests/${id}/resubmit`).then((r) => r.data),

  approvals: (id: string) =>
    apiClient.get(`/api/document-requests/${id}/approvals`).then((r) => r.data),

  inbox: () =>
    apiClient.get<DocumentRequestInboxItem[]>('/api/document-request-approvals/inbox').then((r) => r.data),

  approve: (sheetId: string, comment?: string) =>
    apiClient.post(`/api/document-request-approvals/${sheetId}/approve`, { comment }),

  reject: (sheetId: string, comment: string) =>
    apiClient.post(`/api/document-request-approvals/${sheetId}/reject`, { comment })
};

export type DocumentMatrixStep = { id: string; orderNo: number; approverRole: string };

export type DocumentMatrixSummary = {
  projectOid: string;
  projectName: string | null;
  subcontractorId: string;
  subcontractorName: string;
  requestType: string;
  steps: DocumentMatrixStep[];
};

export const documentMatrixApi = {
  list: () =>
    apiClient.get<DocumentMatrixSummary[]>('/api/document-matrix/summaries').then((r) => r.data),

  get: (projectOid: string, subcontractorId: string, requestType: string) =>
    apiClient
      .get<DocumentMatrixStep[]>('/api/document-matrix', {
        params: { projectOid, subcontractorId, requestType }
      })
      .then((r) => r.data),

  set: (projectOid: string, subcontractorId: string, requestType: string, steps: { orderNo: number; approverRole: string }[]) =>
    apiClient
      .put<DocumentMatrixStep[]>('/api/document-matrix', { steps }, {
        params: { projectOid, subcontractorId, requestType }
      })
      .then((r) => r.data)
};
