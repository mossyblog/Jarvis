// Theme registry for Jarvis Studio UI
export interface ThemeDefinition {
  name: string;
  displayName: string;
  description: string;
  modes: {
    light: boolean;
    dark: boolean;
  };
}

export const themes: Record<string, ThemeDefinition> = {
  default: {
    name: 'default',
    displayName: 'Default',
    description: 'Clean and modern default theme',
    modes: {
      light: true,
      dark: true,
    },
  },
  supabase: {
    name: 'supabase',
    displayName: 'Jarvis',
    description: 'Jarvis green accent theme',
    modes: {
      light: true,
      dark: true,
    },
  },
};

export const getTheme = (name: string): ThemeDefinition | undefined => {
  return themes[name];
};

export const getAvailableThemes = (): ThemeDefinition[] => {
  return Object.values(themes);
};