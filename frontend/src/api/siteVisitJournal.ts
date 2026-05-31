import { apiClient } from './client';

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
    const response = await apiClient.get('/api/site-visit-journal/export', {
      params: { format: format === 'excel' ? 'excel' : 'pdf', ...params },
      responseType: 'blob'
    });
    const blob = response.data as Blob;
    const disposition = response.headers['content-disposition'] as string | undefined;
    const match = disposition?.match(/filename="?([^";]+)"?/);
    const filename = match?.[1] ?? `journal.${format === 'excel' ? 'csv' : 'pdf'}`;
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
};
