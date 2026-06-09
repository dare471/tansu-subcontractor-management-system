import { apiClient } from './client';

export type ApproverDelegation = {
  id: string;
  delegatorUserId: string;
  delegatorName: string;
  delegateUserId: string;
  delegateName: string;
  projectOid: string | null;
  subcontractorId: string | null;
  approverRole: string | null;
  validFrom: string;
  validTo: string;
  isActive: boolean;
};

export const delegationsApi = {
  list(activeOnly = true) {
    return apiClient
      .get<ApproverDelegation[]>('/api/delegations', { params: { activeOnly } })
      .then((r) => r.data);
  },
  create(body: {
    delegateUserId: string;
    projectOid?: string;
    subcontractorId?: string;
    approverRole?: string;
    validFrom: string;
    validTo: string;
  }) {
    return apiClient.post<ApproverDelegation>('/api/delegations', body).then((r) => r.data);
  },
  revoke(id: string) {
    return apiClient.delete(`/api/delegations/${id}`);
  }
};
