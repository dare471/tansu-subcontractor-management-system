import { apiClient } from './client';

export type AuditEvent = {
  id: string;
  occurredAt: string;
  actorUserId: string | null;
  actorEmail: string | null;
  actorType: string;
  action: string;
  entityType: string;
  entityId: string;
  projectOid: string | null;
  subcontractorId: string | null;
  summary: string;
  payloadJson: string | null;
};

export type AuditEventsPage = {
  items: AuditEvent[];
  total: number;
  page: number;
  pageSize: number;
};

export const auditApi = {
  list(params: {
    page?: number;
    pageSize?: number;
    action?: string;
    entityType?: string;
    from?: string;
    to?: string;
  } = {}) {
    return apiClient.get<AuditEventsPage>('/api/audit-events', { params }).then((r) => r.data);
  }
};
