import React, { createContext, useContext, useState, useEffect, useMemo } from 'react';

export type ThemeMode = 'light' | 'dark';
export type ThemeName = 'default' | 'supabase';

interface ThemeContextValue {
  theme: ThemeName;
  mode: ThemeMode;
  setTheme: (theme: ThemeName) => void;
  setMode: (mode: ThemeMode) => void;
  availableThemes: readonly ThemeName[];
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

const THEME_STORAGE_KEY = 'jarvis-theme';
const MODE_STORAGE_KEY = 'jarvis-theme-mode';

interface ThemeProviderProps {
  children: React.ReactNode;
  defaultTheme?: ThemeName;
  defaultMode?: ThemeMode;
}

export function ThemeProvider({ 
  children, 
  defaultTheme = 'supabase',
  defaultMode = 'dark' 
}: ThemeProviderProps) {
  const availableThemes = useMemo(() => ['default', 'supabase'] as const, []);
  
  const [theme, setThemeState] = useState<ThemeName>(() => {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    return (stored && availableThemes.includes(stored as ThemeName)) 
      ? stored as ThemeName 
      : defaultTheme;
  });
  
  const [mode, setModeState] = useState<ThemeMode>(() => {
    const stored = localStorage.getItem(MODE_STORAGE_KEY);
    return stored === 'light' || stored === 'dark' ? stored : defaultMode;
  });

  const setTheme = (newTheme: ThemeName) => {
    setThemeState(newTheme);
    localStorage.setItem(THEME_STORAGE_KEY, newTheme);
  };

  const setMode = (newMode: ThemeMode) => {
    setModeState(newMode);
    localStorage.setItem(MODE_STORAGE_KEY, newMode);
  };

  useEffect(() => {
    // Apply theme classes to root element
    const root = document.documentElement;
    
    // Remove all theme classes
    availableThemes.forEach(t => root.classList.remove(t));
    root.classList.remove('light', 'dark');
    
    // Add current theme classes
    root.classList.add(theme, mode);
    
    // Set data attributes for CSS
    root.setAttribute('data-theme', theme);
    root.setAttribute('data-mode', mode);
  }, [theme, mode, availableThemes]);

  const value: ThemeContextValue = {
    theme,
    mode,
    setTheme,
    setMode,
    availableThemes
  };

  return (
    <ThemeContext.Provider value={value}>
      {children}
    </ThemeContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useTheme() {
  const context = useContext(ThemeContext);
  if (context === undefined) {
    throw new Error('useTheme must be used within a ThemeProvider');
  }
  return context;
}