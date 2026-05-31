import { apiClient } from './client';

export type Subcontractor = {
  id: string;
  name: string;
  bin: string;
  projectsCount: number;
  employeesApprovedCount: number;
  employeesNotApprovedCount: number;
  createdAt: string;
};

export type ProjectBinding = {
  projectOid: string;
  name: string | null;
  activityType: string;
  completionPercent: number;
  progressReportedAt: string | null;
};

export const subcontractorsApi = {
  list: (search?: string) =>
    apiClient.get<Subcontractor[]>('/api/subcontractors', { params: { search } }).then((r) => r.data),
  create: (name: string, bin: string) =>
    apiClient.post<Subcontractor>('/api/subcontractors', { name, bin }).then((r) => r.data),
  update: (id: string, name: string, bin: string) =>
    apiClient.put<Subcontractor>(`/api/subcontractors/${id}`, { name, bin }).then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/api/subcontractors/${id}`),
  projects: (id: string) =>
    apiClient.get<ProjectBinding[]>(`/api/subcontractors/${id}/projects`).then((r) => r.data),
  bindProject: (id: string, projectOid: string, activityType: string, projectName?: string) =>
    apiClient.post(`/api/subcontractors/${id}/projects`, { projectOid, projectName, activityType }),
  unbindProject: (id: string, projectOid: string) =>
    apiClient.delete(`/api/subcontractors/${id}/projects/${projectOid}`)
};
