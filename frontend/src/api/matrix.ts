import { apiClient } from './client';

export type MatrixStep = {
  id: string;
  orderNo: number;
  userId: string;
  userFullName: string;
  userEmail: string;
};

export type MatrixSummary = {
  projectOid: string;
  projectName: string | null;
  subcontractorId: string;
  subcontractorName: string;
  steps: MatrixStep[];
};

export const matrixApi = {
  list: () =>
    apiClient.get<MatrixSummary[]>('/api/approval-matrix').then((r) => r.data),

  get: (projectOid: string, subcontractorId: string) =>
    apiClient
      .get<MatrixStep[]>(`/api/projects/${projectOid}/subcontractors/${subcontractorId}/matrix`)
      .then((r) => r.data),
  set: (projectOid: string, subcontractorId: string, steps: { orderNo: number; userId: string }[]) =>
    apiClient
      .put<MatrixStep[]>(
        `/api/projects/${projectOid}/subcontractors/${subcontractorId}/matrix`,
        { steps }
      )
      .then((r) => r.data)
};
