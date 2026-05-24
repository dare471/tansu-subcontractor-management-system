import { apiClient } from './client';

export type InboxItem = {
  sheetId: string;
  employeeId: string;
  employeeFullName: string;
  position: string;
  subcontractorId: string;
  subcontractorName: string;
  projectOid: string;
  projectName: string | null;
  orderNo: number;
  submittedAt: string;
  batchId: string | null;
  batchTitle: string | null;
};

export const approvalsApi = {
  inbox: () => apiClient.get<InboxItem[]>('/api/approvals/inbox').then((r) => r.data),
  approve: (sheetId: string, comment?: string) =>
    apiClient.post(`/api/approvals/${sheetId}/approve`, { comment: comment ?? null }),
  reject: (sheetId: string, comment: string) =>
    apiClient.post(`/api/approvals/${sheetId}/reject`, { comment })
};
