import { apiClient } from './client';

export type Subcontractor = {
  id: string;
  name: string;
  bin: string;
  projectsCount: number;
  employeesApprovedCount: number;
  employeesNotApprovedCount: number;
  isActive: boolean;
  managerUserId: string | null;
  managerFullName: string | null;
  createdAt: string;
};

export type SubcontractorDocument = {
  id: string;
  name: string;
  documentType: string;
  documentTypeLabel: string;
  contentType: string | null;
  uploadedAt: string;
  uploadedByFullName: string;
};

export type ProjectBinding = {
  projectOid: string;
  name: string | null;
  activityType: string;
  completionPercent: number;
  progressReportedAt: string | null;
};

export const SUBCONTRACTOR_DOC_TYPES = [
  { label: 'Договор', value: 'contract' },
  { label: 'Лицензия / допуск', value: 'license' },
  { label: 'Страхование', value: 'insurance' },
  { label: 'Учредительные документы', value: 'charter' },
  { label: 'Иной документ', value: 'other' }
];

export const subcontractorsApi = {
  list: (search?: string) =>
    apiClient.get<Subcontractor[]>('/api/subcontractors', { params: { search } }).then((r) => r.data),
  create: (
    name: string,
    bin: string,
    options?: { projectOid?: string; projectName?: string; activityType?: string }
  ) =>
    apiClient.post<Subcontractor>('/api/subcontractors', {
      name,
      bin,
      projectOid: options?.projectOid,
      projectName: options?.projectName,
      activityType: options?.activityType
    }).then((r) => r.data),
  update: (id: string, payload: { name: string; bin: string; managerUserId?: string | null }) =>
    apiClient.put<Subcontractor>(`/api/subcontractors/${id}`, payload).then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/api/subcontractors/${id}`),
  projects: (id: string) =>
    apiClient.get<ProjectBinding[]>(`/api/subcontractors/${id}/projects`).then((r) => r.data),
  bindProject: (id: string, projectOid: string, activityType: string, projectName?: string) =>
    apiClient.post(`/api/subcontractors/${id}/projects`, { projectOid, projectName, activityType }),
  unbindProject: (id: string, projectOid: string) =>
    apiClient.delete(`/api/subcontractors/${id}/projects/${projectOid}`),
  documents: (id: string) =>
    apiClient.get<SubcontractorDocument[]>(`/api/subcontractors/${id}/documents`).then((r) => r.data),
  uploadDocument: (id: string, form: FormData) =>
    apiClient.post<SubcontractorDocument>(`/api/subcontractors/${id}/documents`, form).then((r) => r.data),
  documentUrl: (id: string, documentId: string) =>
    `/api/subcontractors/${id}/documents/${documentId}`,
  deleteDocument: (id: string, documentId: string) =>
    apiClient.delete(`/api/subcontractors/${id}/documents/${documentId}`)
};
