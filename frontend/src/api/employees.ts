import { apiClient } from './client';

export type Employee = {
  id: string;
  subcontractorId: string;
  subcontractorName: string;
  projectOid: string;
  projectName: string | null;
  fullName: string;
  position: string;
  phone: string;
  iin: string;
  photoPath: string | null;
  currentStatus: string | null;
  draftBatchId: string | null;
  draftBatchTitle: string | null;
  submittedBatchId: string | null;
  submittedBatchTitle: string | null;
  createdAt: string;
  updatedAt: string;
};

export const employeesApi = {
  list: (params: { projectOid?: string; subcontractorId?: string; search?: string } = {}) =>
    apiClient.get<Employee[]>('/api/employees', { params }).then((r) => r.data),
  create: (projectOid: string, fullName: string, position: string, phone: string, iin: string) =>
    apiClient
      .post<Employee>('/api/employees', { projectOid, fullName, position, phone, iin })
      .then((r) => r.data),
  update: (id: string, fullName: string, position: string, phone: string, iin: string) =>
    apiClient
      .put<Employee>(`/api/employees/${id}`, { fullName, position, phone, iin })
      .then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/api/employees/${id}`),
  uploadPhoto: (id: string, file: File) => {
    const fd = new FormData();
    fd.append('file', file);
    return apiClient
      .post<{ photoPath: string }>(`/api/employees/${id}/photo`, fd)
      .then((r) => r.data);
  },
  photoUrl: (id: string) =>
    `${import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'}/api/employees/${id}/photo`,
  submit: (id: string) =>
    apiClient.post<{ roundId: string }>(`/api/employees/${id}/submit`).then((r) => r.data),
  resubmit: (id: string) =>
    apiClient.post<{ roundId: string }>(`/api/employees/${id}/resubmit`).then((r) => r.data),
  approvals: (id: string) =>
    apiClient.get(`/api/employees/${id}/approvals`).then((r) => r.data)
};
