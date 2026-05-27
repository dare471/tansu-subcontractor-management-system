import { apiClient } from './client';

const photoObjectUrlCache = new Map<string, string>();

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
  invalidatePhotoCache: (id: string) => {
    for (const [key, url] of photoObjectUrlCache) {
      if (key.startsWith(`${id}:`)) {
        URL.revokeObjectURL(url);
        photoObjectUrlCache.delete(key);
      }
    }
  },
  clearPhotoCache: () => {
    for (const url of photoObjectUrlCache.values()) {
      URL.revokeObjectURL(url);
    }
    photoObjectUrlCache.clear();
  },
  fetchPhotoObjectUrl: async (id: string, photoPath: string): Promise<string | null> => {
    const cacheKey = `${id}:${photoPath}`;
    const cached = photoObjectUrlCache.get(cacheKey);
    if (cached) return cached;

    try {
      const res = await apiClient.get(`/api/employees/${id}/photo`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      photoObjectUrlCache.set(cacheKey, url);
      return url;
    } catch {
      return null;
    }
  },
  submit: (id: string) =>
    apiClient.post<{ roundId: string }>(`/api/employees/${id}/submit`).then((r) => r.data),
  resubmit: (id: string) =>
    apiClient.post<{ roundId: string }>(`/api/employees/${id}/resubmit`).then((r) => r.data),
  approvals: (id: string) =>
    apiClient.get(`/api/employees/${id}/approvals`).then((r) => r.data),

  accessPass: (id: string) =>
    apiClient.get<EmployeeAccessPass>(`/api/employees/${id}/access-pass`).then((r) => r.data),

  accessPassQrUrl: (id: string) =>
    `${apiClient.defaults.baseURL}/api/employees/${id}/access-pass/qr.png`,

  siteVisits: (id: string) =>
    apiClient.get<EmployeeSiteVisit[]>(`/api/employees/${id}/site-visits`).then((r) => r.data),

  ppe: (id: string) =>
    apiClient.get<EmployeePpeSummary>(`/api/employees/${id}/ppe`).then((r) => r.data),

  issuePpe: (
    id: string,
    itemType: 'helmet' | 'uniform',
    size?: string,
    inventoryNumber?: string,
    notes?: string
  ) =>
    apiClient
      .post<PpeIssuance>(`/api/employees/${id}/ppe`, { itemType, size, inventoryNumber, notes })
      .then((r) => r.data),

  returnPpe: (id: string, issuanceId: string, notes?: string) =>
    apiClient
      .post<PpeIssuance>(`/api/employees/${id}/ppe/${issuanceId}/return`, { notes })
      .then((r) => r.data)
};

export type EmployeeSiteVisit = {
  id: string;
  employeeId: string;
  employeeFullName: string;
  projectName: string | null;
  checkedInAt: string;
  faceConfidence: number | null;
  verificationMethod: string;
};

export type EmployeeAccessPass = {
  id: string;
  employeeId: string;
  token: string;
  verifyUrl: string;
  issuedAt: string;
  hasReferencePhoto: boolean;
};

export type PpeIssuance = {
  id: string;
  employeeId: string;
  itemType: string;
  size: string | null;
  inventoryNumber: string | null;
  issuedAt: string;
  issuedByFullName: string;
  returnedAt: string | null;
  notes: string | null;
  isActive: boolean;
};

export type EmployeePpeSummary = {
  hasHelmet: boolean;
  hasUniform: boolean;
  activeHelmet: PpeIssuance | null;
  activeUniform: PpeIssuance | null;
  history: PpeIssuance[];
};
