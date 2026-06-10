import { apiClient } from './client';

export type EmployeePortalDashboard = {
  employeeId: string;
  fullName: string;
  position: string;
  workDescription: string;
  subcontractorName: string;
  projectName: string | null;
  approvalStatus: string | null;
  isApproved: boolean;
  safetyQuizCompleted: boolean;
  safetyQuizScore: number | null;
  safetyQuizTotal: number | null;
  canShowQrPass: boolean;
  hasHelmet: boolean;
  hasUniform: boolean;
  accessPass: {
    id: string;
    verifyUrl: string;
    issuedAt: string;
    hasReferencePhoto: boolean;
    qrValidUntil: string;
    passStatus: string;
    employeeBlockStatus: string;
  } | null;
};

export type EmployeePortalProfile = {
  employeeId: string;
  fullName: string;
  position: string;
  phone: string;
  iin: string;
  subcontractorName: string;
  projectName: string | null;
  approvalStatus: string | null;
  hasPhoto: boolean;
  photoReviewStatus: string | null;
  photoReviewReason: string | null;
  accessPassIssuedAt: string | null;
};

export type ApprovalHistoryRow = {
  sheetId: string;
  roundId: string;
  orderNo: number;
  approverUserId: string;
  approverFullName: string;
  status: string;
  comment: string | null;
  decidedAt: string | null;
  createdAt: string;
};

export type ApprovalRound = {
  roundId: string;
  overallStatus: string;
  steps: ApprovalHistoryRow[];
};

export type EmployeeApprovals = {
  employeeId: string;
  currentStatus: string;
  rounds: ApprovalRound[];
};

export type SiteVisitItem = {
  id: string;
  projectName: string | null;
  checkedInAt: string;
  faceConfidence: number | null;
  verificationMethod: string;
};

export type EmployeeSiteVisits = {
  visits: SiteVisitItem[];
  lastCheckedInAt: string | null;
  totalCount: number;
};

export type SafetyQuizQuestion = {
  id: string;
  text: string;
  options: { id: string; text: string }[];
};

export type SafetyQuizResult = {
  passed: boolean;
  score: number;
  total: number;
  message: string;
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

export type PhotoUploadResult = {
  photoPath: string;
  status: string;
  message: string;
};

export const employeePortalApi = {
  dashboard: () =>
    apiClient.get<EmployeePortalDashboard>('/api/employee-portal/dashboard').then((r) => r.data),

  profile: () =>
    apiClient.get<EmployeePortalProfile>('/api/employee-portal/profile').then((r) => r.data),

  approvals: () =>
    apiClient.get<EmployeeApprovals>('/api/employee-portal/approvals').then((r) => r.data),

  siteVisits: () =>
    apiClient.get<EmployeeSiteVisits>('/api/employee-portal/site-visits').then((r) => r.data),

  ppe: () =>
    apiClient.get<EmployeePpeSummary>('/api/employee-portal/ppe').then((r) => r.data),

  quiz: (locale?: string) =>
    apiClient
      .get<SafetyQuizQuestion[]>('/api/employee-portal/safety-quiz', { params: { locale } })
      .then((r) => r.data),

  submitQuiz: (answers: Record<string, string>) =>
    apiClient
      .post<SafetyQuizResult>('/api/employee-portal/safety-quiz', { answers })
      .then((r) => r.data),

  uploadPhoto: (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiClient
      .post<PhotoUploadResult>('/api/employee-portal/photo', form)
      .then((r) => r.data);
  },

  photoBlob: async (): Promise<string | null> => {
    try {
      const res = await apiClient.get('/api/employee-portal/photo', { responseType: 'blob' });
      return URL.createObjectURL(res.data);
    } catch {
      return null;
    }
  },

  qrBlob: async (): Promise<string | null> => {
    try {
      const res = await apiClient.get('/api/employee-portal/access-pass/qr.png', {
        responseType: 'blob'
      });
      return URL.createObjectURL(res.data);
    } catch {
      return null;
    }
  }
};

export function approvalStatusLabel(status: string | null | undefined): string {
  if (!status || status === 'draft') return 'Черновик';
  if (status === 'approved') return 'Согласован';
  if (status === 'rejected') return 'Отклонён';
  if (status === 'pending') return 'На согласовании';
  if (status === 'skipped') return 'Пропущено';
  return status;
}

export function approvalStatusType(status: string | null | undefined): 'success' | 'error' | 'warning' | 'default' {
  if (status === 'approved') return 'success';
  if (status === 'rejected') return 'error';
  if (status === 'pending') return 'warning';
  return 'default';
}

export function ppeItemLabel(type: string): string {
  if (type === 'helmet') return 'Каска';
  if (type === 'uniform') return 'Униформа';
  return type;
}

export function ppeItemIcon(type: string): string {
  if (type === 'helmet') return '⛑️';
  if (type === 'uniform') return '🦺';
  return '📦';
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('ru-RU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}
