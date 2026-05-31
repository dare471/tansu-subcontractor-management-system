import { apiClient } from './client';

export type PendingPhotoReview = {
  employeeId: string;
  fullName: string;
  position: string;
  subcontractorName: string;
  projectName: string | null;
  photoPath: string;
  uploadedAt: string;
  uploadedByUserId: string | null;
  uploadedByFullName: string | null;
  uploadedByEmail: string | null;
  uploadedByUserType: 'TANSU' | 'Subcontractor' | 'Employee' | null;
};

export type EmployeePhotoReview = {
  id: string;
  employeeId: string;
  photoPath: string;
  reviewType: string;
  result: string;
  reason: string | null;
  reviewedByFullName: string | null;
  createdAt: string;
};

export type EmployeePhotoReviewStatus = {
  employeeId: string;
  photoPath: string | null;
  status: string | null;
  reason: string | null;
  canSubmitForApproval: boolean;
  history: EmployeePhotoReview[];
};

export const photoReviewsApi = {
  pending: () =>
    apiClient.get<PendingPhotoReview[]>('/api/employees/photo-reviews/pending').then((r) => r.data),

  status: (employeeId: string) =>
    apiClient
      .get<EmployeePhotoReviewStatus>(`/api/employees/${employeeId}/photo-review`)
      .then((r) => r.data),

  approve: (employeeId: string, comment?: string) =>
    apiClient
      .post<EmployeePhotoReview>(`/api/employees/${employeeId}/photo-review/approve`, { comment })
      .then((r) => r.data),

  reject: (employeeId: string, reason: string) =>
    apiClient
      .post<EmployeePhotoReview>(`/api/employees/${employeeId}/photo-review/reject`, { reason })
      .then((r) => r.data),

  photoUrl: (employeeId: string) =>
    `${apiClient.defaults.baseURL}/api/employees/${employeeId}/photo`
};
