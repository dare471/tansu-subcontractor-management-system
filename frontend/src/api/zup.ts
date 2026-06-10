import { apiClient } from './client';

export type ZupEmployee = {
  externalId: string;
  fullName: string;
  position: string;
  email: string;
  department?: string;
  mobile?: string;
};

export const TANSU_COMPANY_OPTIONS = [
  { label: 'ТОО TANSU Construction', value: 'tansu_construction' },
  { label: 'ТОО KazPromService', value: 'kazprom_service' }
];

export type ZupProject = {
  projectOid: string;
  zupId: number | null;
  code: string | null;
  name: string | null;
  description: string | null;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  customerName: string | null;
  projectManagerName: string | null;
  contractType: string | null;
};

export const zupApi = {
  employees: (company: string) =>
    apiClient.get<ZupEmployee[]>('/api/zup/employees', { params: { company } }).then((r) => r.data),
  projects: () =>
    apiClient.get<ZupProject[]>('/api/zup/projects').then((r) => r.data)
};
