import { apiClient } from './client';

export type SiteIncident = {
  id: string;
  projectOid: string;
  projectName: string | null;
  occurredAt: string;
  title: string;
  description: string;
  severity: string;
  status: string;
  subcontractorId: string | null;
  subcontractorName: string | null;
  blockUntilResolved: boolean;
  resolutionNotes: string | null;
  resolvedAt: string | null;
  employeeIds: string[];
};

export const incidentsApi = {
  list(params: { projectOid?: string; status?: string } = {}) {
    return apiClient.get<SiteIncident[]>('/api/incidents', { params }).then((r) => r.data);
  },
  create(body: {
    projectOid: string;
    occurredAt: string;
    title: string;
    description: string;
    severity: string;
    subcontractorId?: string;
    blockUntilResolved: boolean;
    employeeIds: string[];
  }) {
    return apiClient.post<SiteIncident>('/api/incidents', body).then((r) => r.data);
  },
  updateStatus(id: string, status: string, resolutionNotes?: string) {
    return apiClient.patch<SiteIncident>(`/api/incidents/${id}`, { status, resolutionNotes }).then((r) => r.data);
  }
};
