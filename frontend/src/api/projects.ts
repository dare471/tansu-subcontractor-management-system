import { apiClient } from './client';

export type Project = { projectOid: string; name: string | null; subcontractorsCount: number };

export const projectsApi = {
  list: (search?: string) =>
    apiClient.get<Project[]>('/api/projects', { params: { search } }).then((r) => r.data),
  register: (projectOid: string, name?: string) =>
    apiClient.post<Project>('/api/projects', { projectOid, name }).then((r) => r.data)
};
