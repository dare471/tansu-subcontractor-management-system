import { apiClient } from './client';
import { triggerBlobDownload } from './downloadBlob';

export type SubcontractorCompliance = {
  subcontractorId: string;
  subcontractorName: string;
  totalEmployees: number;
  approvedEmployees: number;
  blockedEmployees: number;
  quizCompleted: number;
  expiringDocuments: number;
};

async function downloadExport(path: string, params: Record<string, string | number | undefined>) {
  const res = await apiClient.get<Blob>(path, { params, responseType: 'blob' });
  triggerBlobDownload(res, String(params.format ?? 'csv'));
}

export const reportsApi = {
  compliance(subcontractorId?: string) {
    return apiClient
      .get<SubcontractorCompliance[]>('/api/reports/subcontractor-compliance', {
        params: { subcontractorId }
      })
      .then((r) => r.data);
  },
  exportApprovedPersonnel(format: 'csv' | 'pdf', params: Record<string, string | undefined> = {}) {
    return downloadExport('/api/reports/approved-personnel/export', { format, ...params });
  },
  exportSiteVisits(format: 'csv' | 'pdf', params: Record<string, string | undefined> = {}) {
    return downloadExport('/api/reports/site-visits/export', { format, ...params });
  },
  exportBlocks(format: 'csv' | 'pdf', params: Record<string, string | undefined> = {}) {
    return downloadExport('/api/reports/employee-blocks/export', { format, ...params });
  },
  exportDocumentRequests(format: 'csv' | 'pdf', params: Record<string, string | undefined> = {}) {
    return downloadExport('/api/reports/document-requests/export', { format, ...params });
  },
  exportExpiringDocuments(format: 'csv' | 'pdf', daysAhead = 14) {
    return downloadExport('/api/reports/expiring-documents/export', { format, daysAhead });
  }
};
