import { apiClient } from './client';
import { triggerBlobDownload } from './downloadBlob';

export type SiteVisitJournalItem = {
  id: string;
  employeeId: string;
  employeeFullName: string;
  employeePosition: string;
  subcontractorName: string;
  projectName: string | null;
  terminalLocation: string | null;
  checkedInAt: string;
  checkedOutAt: string | null;
  dataSource: string;
  dataSourceLabel: string;
  faceConfidence: number | null;
  verificationMethod: string;
};

export type SiteVisitJournalPage = {
  items: SiteVisitJournalItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type SiteVisitJournalFilters = {
  page?: number;
  pageSize?: number;
  search?: string;
  subcontractorId?: string;
  projectOid?: string;
  from?: string;
  to?: string;
};

export const siteVisitJournalApi = {
  list: (params: SiteVisitJournalFilters = {}) =>
    apiClient.get<SiteVisitJournalPage>('/api/site-visit-journal', { params }).then((r) => r.data),

  exportFile: async (format: 'excel' | 'pdf', params: Omit<SiteVisitJournalFilters, 'page' | 'pageSize'> = {}) => {
    const response = await apiClient.get<Blob>('/api/site-visit-journal/export', {
      params: { format: format === 'excel' ? 'excel' : 'pdf', ...params },
      responseType: 'blob'
    });
    triggerBlobDownload(response, format === 'excel' ? 'csv' : 'pdf');
  }
};
