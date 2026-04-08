/**
 * Light-only theme for MVP. Dark mode planned for phase 2.
 * Minimalist design system optimized for low-bandwidth (3G) usage.
 */

export const colors = {
  primary: '#1B5E20',
  primaryLight: '#4C8C4A',
  primaryDark: '#003300',
  secondary: '#FF6F00',
  secondaryLight: '#FFA040',
  background: '#FAFAFA',
  surface: '#FFFFFF',
  error: '#D32F2F',
  errorLight: '#FFEBEE',
  textPrimary: '#212121',
  textSecondary: '#757575',
  textOnPrimary: '#FFFFFF',
  border: '#E0E0E0',
  disabled: '#BDBDBD',
  placeholder: '#9E9E9E',
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
  xxl: 48,
} as const;

export const fontSizes = {
  xs: 12,
  sm: 14,
  md: 16,
  lg: 18,
  xl: 24,
  xxl: 32,
} as const;

export const borderRadius = {
  sm: 4,
  md: 8,
  lg: 12,
  xl: 16,
  full: 9999,
} as const;
