const brandName = (import.meta.env.VITE_APP_BRAND_NAME as string | undefined)?.trim() || 'Tansu';
const companyName = (import.meta.env.VITE_APP_COMPANY_NAME as string | undefined)?.trim() || 'ТАНСУ';

export const appBrand = {
  brandName,
  companyName,
  logoLetter: (brandName[0] ?? 'T').toUpperCase(),
  employeeLabel: `Сотрудник ${companyName}`,
  loginSubtitle: `Вход для сотрудников ${companyName}`,
  teamTab: `Команда ${companyName}`
} as const;
