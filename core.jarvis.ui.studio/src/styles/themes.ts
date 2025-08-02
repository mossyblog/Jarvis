import type { ThemeName } from '../contexts/ThemeContext';

interface ThemeInfo {
  name: ThemeName;
  displayName: string;
  description: string;
}

export const themes: Record<ThemeName, ThemeInfo> = {
  default: {
    name: 'default',
    displayName: 'Default',
    description: 'Clean and modern default theme',
  },
  supabase: {
    name: 'supabase',
    displayName: 'Supabase',
    description: 'Supabase-inspired green theme',
  },
};