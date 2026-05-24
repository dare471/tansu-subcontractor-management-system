import type { GlobalThemeOverrides } from 'naive-ui';

export const brand = {
  orange: '#EE6C1C',
  orangeDark: '#D85C0D',
  orangeLight: '#FFE6D4',
  navy: '#1F2A44',
  navyDark: '#0F1626',
  navyHover: '#2A3958',
  text: '#1F2937',
  textMuted: '#6B7280',
  border: '#E5E7EB',
  bg: '#F4F5F7'
};

export const themeOverrides: GlobalThemeOverrides = {
  common: {
    primaryColor: brand.orange,
    primaryColorHover: brand.orangeDark,
    primaryColorPressed: brand.orangeDark,
    primaryColorSuppl: brand.orangeDark,
    fontWeightStrong: '700',
    borderRadius: '8px',
    bodyColor: brand.bg
  },
  Card: {
    borderRadius: '12px',
    paddingMedium: '20px',
    paddingLarge: '24px'
  },
  Button: {
    fontWeight: '600',
    borderRadiusMedium: '8px'
  },
  Menu: {
    itemHeight: '48px',
    borderRadius: '8px'
  },
  Layout: {
    siderColor: brand.navy,
    headerColor: '#FFFFFF',
    color: brand.bg
  }
};
