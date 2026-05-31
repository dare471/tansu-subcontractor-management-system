import { apiClient } from './client';

export type Project = { projectOid: string; name: string | null; subcontractorsCount: number };

export type ProjectStaffOption = {
  id: string;
  fullName: string;
  email: string;
  tansuRole: string | null;
};

export type ProjectSubcontractorItem = {
  id: string;
  name: string;
  bin: string;
  activityType: string;
  completionPercent: number;
  progressReportedAt: string | null;
  progressReportedByFullName: string | null;
  employeesCount: number;
  approvedEmployeesCount: number;
};

export type ProjectWorkforceItem = {
  employeeId: string;
  fullName: string;
  position: string;
  subcontractorName: string;
  approvalStatus: string | null;
};

export type ProjectTeamMember = {
  userId: string;
  fullName: string;
  email: string;
  position: string | null;
  tansuRole: string | null;
  roleLabel: string;
};

export type ProjectDocument = {
  id: string;
  name: string;
  documentType: string;
  documentTypeLabel: string;
  contentType: string | null;
  uploadedAt: string;
  uploadedByFullName: string;
};

export type ProjectDetail = {
  projectOid: string;
  name: string | null;
  subcontractorsCount: number;
  customerName: string | null;
  customerPhone: string | null;
  customerEmail: string | null;
  budgetAmount: number | null;
  budgetCurrency: string;
  responsibleAdminUserId: string | null;
  responsibleAdminFullName: string | null;
  responsibleAdminEmail: string | null;
  projectManagerUserId: string | null;
  projectManagerFullName: string | null;
  projectManagerEmail: string | null;
  subcontractors: ProjectSubcontractorItem[];
  workforce: ProjectWorkforceItem[];
  team: ProjectTeamMember[];
  documents: ProjectDocument[];
};

export type UpdateProjectPayload = {
  name?: string | null;
  customerName?: string | null;
  customerPhone?: string | null;
  customerEmail?: string | null;
  budgetAmount?: number | null;
  budgetCurrency?: string | null;
  responsibleAdminUserId?: string | null;
  projectManagerUserId?: string | null;
};

export const PROJECT_DOCUMENT_TYPES = [
  { label: 'Договор', value: 'contract' },
  { label: 'Смета', value: 'estimate' },
  { label: 'Акт', value: 'act' },
  { label: 'Разрешение', value: 'permit' },
  { label: 'Прочее', value: 'other' }
];

export const projectsApi = {
  list: (search?: string) =>
    apiClient.get<Project[]>('/api/projects', { params: { search } }).then((r) => r.data),
  register: (projectOid: string, name?: string) =>
    apiClient.post<Project>('/api/projects', { projectOid, name }).then((r) => r.data),
  get: (projectOid: string) =>
    apiClient.get<ProjectDetail>(`/api/projects/${projectOid}`).then((r) => r.data),
  update: (projectOid: string, payload: UpdateProjectPayload) =>
    apiClient.put<ProjectDetail>(`/api/projects/${projectOid}`, payload).then((r) => r.data),
  staffOptions: () =>
    apiClient.get<ProjectStaffOption[]>('/api/projects/staff-options').then((r) => r.data),
  uploadDocument: (projectOid: string, file: File, name: string, documentType: string) => {
    const form = new FormData();
    form.append('file', file);
    form.append('name', name);
    form.append('documentType', documentType);
    return apiClient
      .post<ProjectDocument>(`/api/projects/${projectOid}/documents`, form)
      .then((r) => r.data);
  },
  documentUrl: (projectOid: string, documentId: string) =>
    `${apiClient.defaults.baseURL}/api/projects/${projectOid}/documents/${documentId}`,
  deleteDocument: (projectOid: string, documentId: string) =>
    apiClient.delete(`/api/projects/${projectOid}/documents/${documentId}`),
  bindSubcontractor: (projectOid: string, subcontractorId: string, activityType: string) =>
    apiClient.post(`/api/projects/${projectOid}/subcontractors`, { subcontractorId, activityType }),
  updateSubcontractorBinding: (projectOid: string, subcontractorId: string, activityType: string) =>
    apiClient.put(`/api/projects/${projectOid}/subcontractors/${subcontractorId}`, { activityType })
};

export function formatBudget(amount: number | null, currency: string) {
  if (amount == null) return '—';
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: currency || 'KZT',
    maximumFractionDigits: 0
  }).format(amount);
}

export function approvalStatusLabel(status: string | null) {
  if (!status) return 'Черновик';
  if (status === 'approved') return 'В работе';
  if (status === 'pending') return 'На согласовании';
  if (status === 'rejected') return 'Отклонён';
  return status;
}
