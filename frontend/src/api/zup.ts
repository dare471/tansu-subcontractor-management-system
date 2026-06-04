import { apiClient } from './client';

export type ZupEmployee = {
  externalId: string;
  fullName: string;
  position: string;
  email: string;
};

export const TANSU_COMPANY_OPTIONS = [
  { label: 'ТОО TANSU Construction', value: 'tansu_construction' },
  { label: 'ТОО KazPromService', value: 'kazprom_service' }
];

export const zupApi = {
  employees: (company: string) =>
    apiClient.get<ZupEmployee[]>('/api/zup/employees', { params: { company } }).then((r) => r.data)
};
